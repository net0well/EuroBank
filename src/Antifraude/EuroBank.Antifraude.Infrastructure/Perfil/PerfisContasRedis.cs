using System.Globalization;
using EuroBank.Antifraude.Application.Interfaces;
using EuroBank.Antifraude.Application.TransferObjects;
using EuroBank.Antifraude.Domain.Policies;
using EuroBank.Antifraude.Domain.ValueObjects;
using StackExchange.Redis;

namespace EuroBank.Antifraude.Infrastructure.Perfil;

/// <summary>
/// Perfil de comportamento no Redis. A escrita é um script Lua: confere a versão da conta e aplica tudo (saldo,
/// abertura, saída, beneficiário, limpeza das saídas antigas) de uma vez, sem corrida entre instâncias do worker.
/// A leitura busca as três chaves num lote só (uma ida ao Redis).
/// </summary>
public sealed class PerfisContasRedis(IConnectionMultiplexer redis) : IPerfisContas
{
    /// <summary>Saídas guardadas: um dia a mais que a maior janela das regras (30 dias).</summary>
    public static readonly TimeSpan Retencao = TimeSpan.FromDays(31);

    // Parâmetros @nome: o LuaScript do StackExchange.Redis os troca por KEYS e ARGV a partir do objeto passado.
    private static readonly LuaScript Aplicar = LuaScript.Prepare("""
        local atual = tonumber(redis.call('HGET', @dados, 'versao') or '0')
        if tonumber(@versao) <= atual then
          return 0
        end
        redis.call('HSET', @dados, 'versao', @versao)
        if @saldo ~= '' then redis.call('HSET', @dados, 'saldo', @saldo) end
        if @abertaEm ~= '' then redis.call('HSET', @dados, 'aberta_em', @abertaEm) end
        if @moeda ~= '' then redis.call('HSET', @dados, 'moeda', @moeda) end
        if @score ~= '' then
          redis.call('ZADD', @saidas, @score, @membro)
          redis.call('ZREMRANGEBYSCORE', @saidas, '-inf', '(' .. @corte)
          redis.call('SADD', @beneficiarios, @iban)
        end
        return 1
        """);

    public async Task<bool> AplicarAsync(AtualizacaoPerfil atualizacao, CancellationToken cancellationToken)
    {
        var saida = atualizacao.Saida;
        var iban = saida is null ? string.Empty : NormalizacaoIban.Normalizar(saida.IbanDestino) ?? string.Empty;
        var resultado = await redis.GetDatabase().ScriptEvaluateAsync(Aplicar, new
        {
            dados = (RedisKey)ChavesPerfil.Dados(atualizacao.ContaId),
            saidas = (RedisKey)ChavesPerfil.Saidas(atualizacao.ContaId),
            beneficiarios = (RedisKey)ChavesPerfil.Beneficiarios(atualizacao.ContaId),
            versao = atualizacao.Versao,
            saldo = Texto(atualizacao.SaldoApos),
            abertaEm = atualizacao.AbertaEm is { } abertaEm ? abertaEm.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture) : string.Empty,
            moeda = atualizacao.Moeda ?? string.Empty,
            score = saida is null ? string.Empty : atualizacao.OcorridoEm.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture),
            membro = saida is null ? string.Empty : $"{saida.TransferenciaId}|{Texto(saida.Valor)}|{iban}",
            iban,
            corte = (atualizacao.OcorridoEm - Retencao).ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture),
        });
        return (int)resultado == 1;
    }

    public async Task<PerfilConta> ObterAsync(Guid contaId, DateTimeOffset agora, CancellationToken cancellationToken)
    {
        var lote = redis.GetDatabase().CreateBatch();
        var dados = lote.HashGetAllAsync(ChavesPerfil.Dados(contaId));
        var saidas = lote.SortedSetRangeByScoreWithScoresAsync(
            ChavesPerfil.Saidas(contaId), start: (agora - Retencao).ToUnixTimeMilliseconds());
        var beneficiarios = lote.SetMembersAsync(ChavesPerfil.Beneficiarios(contaId));
        lote.Execute();
        await Task.WhenAll(dados, saidas, beneficiarios);

        var campos = dados.Result.ToDictionary(campo => campo.Name.ToString(), campo => campo.Value.ToString(), StringComparer.Ordinal);
        if (campos.Count == 0)
        {
            return PerfilConta.Vazio(contaId);
        }

        return new PerfilConta(
            contaId,
            campos.TryGetValue("aberta_em", out var abertaEm) ? DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(abertaEm, CultureInfo.InvariantCulture)) : null,
            campos.TryGetValue("saldo", out var saldo) ? decimal.Parse(saldo, CultureInfo.InvariantCulture) : null,
            saidas.Result.Select(LerSaida).ToList(),
            beneficiarios.Result.Select(membro => membro.ToString()).ToHashSet(StringComparer.Ordinal));
    }

    private static SaidaConta LerSaida(SortedSetEntry entrada)
    {
        var partes = entrada.Element.ToString().Split('|');
        return new SaidaConta(
            DateTimeOffset.FromUnixTimeMilliseconds((long)entrada.Score),
            decimal.Parse(partes[1], CultureInfo.InvariantCulture),
            partes[2].Length == 0 ? null : partes[2]);
    }

    private static string Texto(decimal? valor) => valor?.ToString("0.00##", CultureInfo.InvariantCulture) ?? string.Empty;
}
