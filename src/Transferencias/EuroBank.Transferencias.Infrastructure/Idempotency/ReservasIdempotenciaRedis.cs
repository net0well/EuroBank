using EuroBank.Transferencias.Application.Enums;
using EuroBank.Transferencias.Application.Interfaces;
using EuroBank.Transferencias.Application.TransferObjects;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace EuroBank.Transferencias.Infrastructure.Idempotency;

/// <summary>
/// Cada key é um hash em idempotencia:transferencias:{key} com o estado (andamento ou concluida), a impressão digital
/// do pedido e, depois de gravada, a transferência. A reserva vale 30 s (se o pedido cair no meio, a key se solta
/// sozinha); a conclusão, 24 h. Falha do Redis vira <see cref="SituacaoReserva.Indisponivel"/>.
/// </summary>
public sealed class ReservasIdempotenciaRedis(IConnectionMultiplexer redis, ILogger<ReservasIdempotenciaRedis> logger)
    : IReservasIdempotencia
{
    private static readonly TimeSpan ValidadeDaReserva = TimeSpan.FromSeconds(30);

    private static readonly TimeSpan ValidadeDaConclusao = TimeSpan.FromHours(24);

    private const string Reservar = """
        local estado = redis.call('HGET', KEYS[1], 'estado')
        if not estado then
          redis.call('HSET', KEYS[1], 'estado', 'andamento', 'hash', ARGV[1])
          redis.call('PEXPIRE', KEYS[1], ARGV[2])
          return {'reservada', ARGV[1]}
        end
        return {estado, redis.call('HGET', KEYS[1], 'hash')}
        """;

    public async Task<ReservaIdempotencia> ReservarAsync(string chave, string hashDoPedido, CancellationToken cancellationToken)
    {
        try
        {
            var resultado = (RedisResult[])(await redis.GetDatabase().ScriptEvaluateAsync(
                Reservar, [Chave(chave)], [hashDoPedido, (long)ValidadeDaReserva.TotalMilliseconds]))!;
            return new ReservaIdempotencia(Situacao(resultado[0].ToString()), resultado[1].ToString());
        }
        catch (Exception erro) when (erro is RedisException or RedisTimeoutException)
        {
            logger.LogWarning(erro, "Redis indisponível para a Idempotency-Key {Chave}: vale só a constraint do Postgres", chave);
            return new ReservaIdempotencia(SituacaoReserva.Indisponivel);
        }
    }

    public async Task<ReservaIdempotencia> ConsultarAsync(string chave, CancellationToken cancellationToken)
    {
        try
        {
            var campos = await redis.GetDatabase().HashGetAsync(Chave(chave), ["estado", "hash"]);
            return campos[0].IsNull
                ? new ReservaIdempotencia(SituacaoReserva.Livre)
                : new ReservaIdempotencia(Situacao(campos[0].ToString()), campos[1].ToString());
        }
        catch (Exception erro) when (erro is RedisException or RedisTimeoutException)
        {
            logger.LogWarning(erro, "Redis indisponível ao consultar a Idempotency-Key {Chave}", chave);
            return new ReservaIdempotencia(SituacaoReserva.Indisponivel);
        }
    }

    public async Task ConcluirAsync(string chave, string hashDoPedido, Guid transferenciaId, CancellationToken cancellationToken)
    {
        try
        {
            var banco = redis.GetDatabase();
            var transacao = banco.CreateTransaction();
            _ = transacao.HashSetAsync(
                Chave(chave), [new HashEntry("estado", "concluida"), new HashEntry("hash", hashDoPedido), new HashEntry("transferencia", transferenciaId.ToString())]);
            _ = transacao.KeyExpireAsync(Chave(chave), ValidadeDaConclusao);
            await transacao.ExecuteAsync();
        }
        catch (Exception erro) when (erro is RedisException or RedisTimeoutException)
        {
            // A transferência já está no Postgres: um repetido a encontra lá pela constraint.
            logger.LogWarning(erro, "Redis indisponível ao concluir a Idempotency-Key {Chave}", chave);
        }
    }

    public async Task LiberarAsync(string chave, CancellationToken cancellationToken)
    {
        try
        {
            await redis.GetDatabase().KeyDeleteAsync(Chave(chave));
        }
        catch (Exception erro) when (erro is RedisException or RedisTimeoutException)
        {
            logger.LogWarning(erro, "Redis indisponível ao soltar a Idempotency-Key {Chave}; ela expira em {Validade}", chave, ValidadeDaReserva);
        }
    }

    private static RedisKey Chave(string chave) => $"idempotencia:transferencias:{chave}";

    private static SituacaoReserva Situacao(string estado) => estado switch
    {
        "reservada" => SituacaoReserva.Reservada,
        "andamento" => SituacaoReserva.EmAndamento,
        "concluida" => SituacaoReserva.Concluida,
        _ => throw new InvalidOperationException($"Estado de Idempotency-Key desconhecido: {estado}"),
    };
}
