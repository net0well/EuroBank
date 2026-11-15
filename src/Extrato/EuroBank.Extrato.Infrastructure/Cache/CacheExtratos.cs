using System.Globalization;
using System.Text.Json;
using EuroBank.Extrato.Application.TransferObjects;
using EuroBank.Extrato.Infrastructure.Cache.TransferObjects;
using EuroBank.Extrato.Infrastructure.Enums;
using EuroBank.Extrato.Infrastructure.Options;
using StackExchange.Redis;

namespace EuroBank.Extrato.Infrastructure.Cache;

/// <summary>
/// O resumo e os últimos lançamentos de cada conta no Redis. As falhas do Redis (fora do ar, timeout) sobem para quem
/// chama, que decide entre ler do Postgres e marcar o cache como suspeito.
/// </summary>
public sealed class CacheExtratos(IConnectionMultiplexer redis, TimeProvider relogio)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>O retrato guardado com até <paramref name="quantidade"/> lançamentos, ou nulo se a conta não está no cache.</summary>
    public async Task<RetratoExtrato?> ObterAsync(Guid contaId, int quantidade)
    {
        var lote = redis.GetDatabase().CreateBatch();
        var resumo = lote.HashGetAllAsync(ChavesExtrato.Resumo(contaId));
        var lancamentos = lote.ListRangeAsync(ChavesExtrato.Lancamentos(contaId), 0, quantidade - 1);
        lote.Execute();
        await Task.WhenAll(resumo, lancamentos);

        var campos = Ler(await resumo);
        if (campos is null)
        {
            return null;
        }

        return new RetratoExtrato(
            Estado(campos),
            long.Parse(campos["total"], CultureInfo.InvariantCulture),
            (await lancamentos).Select(item => JsonSerializer.Deserialize<LancamentoDto>(item.ToString(), Json)!).ToList(),
            DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(campos["gravado_em"], CultureInfo.InvariantCulture)));
    }

    /// <summary>Só o resumo, para o write-behind reconstituir o agregado.</summary>
    public async Task<EstadoExtrato?> ObterEstadoAsync(Guid contaId) =>
        Ler(await redis.GetDatabase().HashGetAllAsync(ChavesExtrato.Resumo(contaId))) is { } campos ? Estado(campos) : null;

    /// <summary>Grava o retrato lido do Postgres; um retrato mais novo que já esteja lá é mantido.</summary>
    public async Task<bool> GravarRetratoAsync(RetratoExtrato retrato, TimeSpan? ttl)
    {
        ArgumentNullException.ThrowIfNull(retrato);
        var argumentos = new List<RedisValue>
        {
            retrato.Estado.Versao,
            Milissegundos(ttl),
            relogio.GetUtcNow().ToUnixTimeMilliseconds(),
            retrato.Total,
        };
        argumentos.AddRange(Campos(retrato.Estado));
        argumentos.AddRange(retrato.Lancamentos.Take(OpcoesCache.LancamentosNoCache).Select(lancamento => (RedisValue)JsonSerializer.Serialize(lancamento, Json)));

        var resultado = await redis.GetDatabase().ScriptEvaluateAsync(
            ScriptsCache.GravarRetrato,
            [ChavesExtrato.Resumo(retrato.Estado.ContaId), ChavesExtrato.Lancamentos(retrato.Estado.ContaId)],
            [.. argumentos]);
        return (int)resultado == 1;
    }

    /// <summary>
    /// Aplica o estado depois de um evento e os lançamentos novos (do mais antigo para o mais recente). Com
    /// <paramref name="pendencia"/>, registra no mesmo script o que o write-behind ainda deve ao Postgres.
    /// </summary>
    public async Task<ResultadoEscritaCache> AplicarAsync(
        EstadoExtrato estado,
        IReadOnlyList<LancamentoDto> novos,
        bool exigirExistente,
        TimeSpan? ttl,
        PendenciaExtrato? pendencia = null)
    {
        ArgumentNullException.ThrowIfNull(estado);
        ArgumentNullException.ThrowIfNull(novos);
        var argumentos = new List<RedisValue>
        {
            estado.Versao,
            exigirExistente ? "1" : "0",
            Milissegundos(ttl),
            relogio.GetUtcNow().ToUnixTimeMilliseconds(),
            pendencia is null ? string.Empty : JsonSerializer.Serialize(pendencia, Json),
            OpcoesCache.LancamentosNoCache,
        };
        argumentos.AddRange(Campos(estado));
        argumentos.AddRange(novos.Select(lancamento => (RedisValue)JsonSerializer.Serialize(lancamento, Json)));

        var resultado = await redis.GetDatabase().ScriptEvaluateAsync(
            ScriptsCache.Aplicar,
            [ChavesExtrato.Resumo(estado.ContaId), ChavesExtrato.Lancamentos(estado.ContaId), ChavesExtrato.Pendencias],
            [.. argumentos]);
        return (ResultadoEscritaCache)(int)resultado;
    }

    public Task RemoverAsync(Guid contaId) =>
        redis.GetDatabase().KeyDeleteAsync([ChavesExtrato.Resumo(contaId), ChavesExtrato.Lancamentos(contaId)]);

    /// <summary>Apaga o resumo e os lançamentos de todas as contas (o stream de pendências fica). Devolve quantas chaves.</summary>
    public async Task<long> RemoverTodosAsync(CancellationToken cancellationToken)
    {
        var banco = redis.GetDatabase();
        long removidas = 0;
        foreach (var servidor in redis.GetServers().Where(servidor => !servidor.IsReplica))
        {
            foreach (var padrao in new[] { ChavesExtrato.PadraoResumo, ChavesExtrato.PadraoLancamentos })
            {
                var lote = new List<RedisKey>(500);
                await foreach (var chave in servidor.KeysAsync(pattern: padrao, pageSize: 500).WithCancellation(cancellationToken))
                {
                    lote.Add(chave);
                    if (lote.Count == 500)
                    {
                        removidas += await banco.KeyDeleteAsync([.. lote]);
                        lote.Clear();
                    }
                }

                if (lote.Count > 0)
                {
                    removidas += await banco.KeyDeleteAsync([.. lote]);
                }
            }
        }

        return removidas;
    }

    public Task<TimeSpan> PingAsync() => redis.GetDatabase().PingAsync();

    private static Dictionary<string, string>? Ler(HashEntry[] entradas) =>
        entradas.Length == 0 ? null : entradas.ToDictionary(entrada => entrada.Name.ToString(), entrada => entrada.Value.ToString(), StringComparer.Ordinal);

    private static EstadoExtrato Estado(Dictionary<string, string> campos) => new(
        Guid.Parse(campos["conta_id"]),
        campos["iban"],
        campos["titular"],
        campos["status"],
        campos["moeda"],
        decimal.Parse(campos["saldo"], CultureInfo.InvariantCulture),
        int.Parse(campos["versao"], CultureInfo.InvariantCulture),
        DateTimeOffset.Parse(campos["atualizado_em"], CultureInfo.InvariantCulture));

    /// <summary>A ordem que os dois scripts esperam: conta, IBAN, titular, status, moeda, saldo e atualizado em.</summary>
    private static RedisValue[] Campos(EstadoExtrato estado) =>
    [
        estado.ContaId.ToString(),
        estado.Iban,
        estado.Titular,
        estado.Status,
        estado.Moeda,
        estado.Saldo.ToString("0.00", CultureInfo.InvariantCulture),
        estado.AtualizadoEm.ToString("O", CultureInfo.InvariantCulture),
    ];

    private static long Milissegundos(TimeSpan? ttl) => ttl is { } valor ? (long)valor.TotalMilliseconds : 0;
}
