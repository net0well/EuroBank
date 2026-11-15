using EuroBank.Transferencias.Application.TransferObjects;

namespace EuroBank.Transferencias.Application.Interfaces;

/// <summary>
/// A Idempotency-Key no Redis, antes da constraint única do Postgres: o primeiro pedido reserva a key (por pouco tempo,
/// se ele cair no meio) e, ao gravar, a marca como concluída por 24 h. Com o Redis fora, tudo responde
/// <see cref="Enums.SituacaoReserva.Indisponivel"/> e vale só o Postgres.
/// </summary>
public interface IReservasIdempotencia
{
    Task<ReservaIdempotencia> ReservarAsync(string chave, string hashDoPedido, CancellationToken cancellationToken);

    /// <summary>Só lê a situação, sem reservar.</summary>
    Task<ReservaIdempotencia> ConsultarAsync(string chave, CancellationToken cancellationToken);

    Task ConcluirAsync(string chave, string hashDoPedido, Guid transferenciaId, CancellationToken cancellationToken);

    /// <summary>Solta a key de um pedido que falhou, para quem repetir não esperar à toa.</summary>
    Task LiberarAsync(string chave, CancellationToken cancellationToken);
}
