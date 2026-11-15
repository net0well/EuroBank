using System.Text.Json;
using EuroBank.Simuladores.Cambio.TransferObjects;
using StackExchange.Redis;

namespace EuroBank.Simuladores.Cambio.Persistencia;

/// <summary>
/// A cotação válida de cada transferência no Redis (cambio:cotacao:{transferência}), com expiração igual à validade:
/// um CotarCambio repetido (reenvio da saga) recebe a mesma cotação enquanto ela vale. Sem a chave no Redis, ou com o
/// Redis fora, a tabela cotacoes responde; vencida, o simulador cota de novo.
/// </summary>
public sealed class CotacoesValidas(
    IConnectionMultiplexer redis,
    RepositorioCambio repositorio,
    ILogger<CotacoesValidas> logger)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>A cotação ainda válida para o mesmo par, ou nula.</summary>
    public async Task<CotacaoEmitida?> ObterAsync(CotarCambio pedido, DateTimeOffset agora, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pedido);
        try
        {
            var guardada = await redis.GetDatabase().StringGetAsync(Chave(pedido.TransferenciaId));
            if (!guardada.IsNullOrEmpty
                && JsonSerializer.Deserialize<CotacaoEmitida>(guardada.ToString(), Json) is { } cotacao
                && Serve(cotacao, pedido, agora))
            {
                return cotacao;
            }
        }
        catch (Exception erro) when (erro is RedisException or RedisTimeoutException)
        {
            logger.LogWarning(erro, "Redis indisponível: a cotação da transferência {TransferenciaId} vem da tabela", pedido.TransferenciaId);
            return await DaTabelaAsync(pedido, agora, guardarNoRedis: false, cancellationToken);
        }

        return await DaTabelaAsync(pedido, agora, guardarNoRedis: true, cancellationToken);
    }

    /// <summary>Guarda a cotação recém-emitida até ela vencer. Falhar aqui só faz o próximo pedido ler da tabela.</summary>
    public async Task GuardarAsync(CotacaoEmitida cotacao, DateTimeOffset agora)
    {
        ArgumentNullException.ThrowIfNull(cotacao);
        var validade = cotacao.ValidaAte - agora;
        if (validade <= TimeSpan.Zero)
        {
            return;
        }

        try
        {
            await redis.GetDatabase().StringSetAsync(Chave(cotacao.TransferenciaId), JsonSerializer.Serialize(cotacao, Json), validade);
        }
        catch (Exception erro) when (erro is RedisException or RedisTimeoutException)
        {
            logger.LogWarning(erro, "Redis indisponível ao guardar a cotação {CotacaoId}", cotacao.CotacaoId);
        }
    }

    private async Task<CotacaoEmitida?> DaTabelaAsync(CotarCambio pedido, DateTimeOffset agora, bool guardarNoRedis, CancellationToken cancellationToken)
    {
        var cotacao = await repositorio.ObterValidaAsync(pedido.TransferenciaId, agora, cancellationToken);
        if (cotacao is null || !Serve(cotacao, pedido, agora))
        {
            return null;
        }

        if (guardarNoRedis)
        {
            await GuardarAsync(cotacao, agora);
        }

        return cotacao;
    }

    private static bool Serve(CotacaoEmitida cotacao, CotarCambio pedido, DateTimeOffset agora) =>
        cotacao.ValidaAte > agora
        && string.Equals(cotacao.MoedaOrigem, pedido.MoedaOrigem, StringComparison.OrdinalIgnoreCase)
        && string.Equals(cotacao.MoedaDestino, pedido.MoedaDestino, StringComparison.OrdinalIgnoreCase);

    private static RedisKey Chave(Guid transferenciaId) => $"cambio:cotacao:{transferenciaId}";
}
