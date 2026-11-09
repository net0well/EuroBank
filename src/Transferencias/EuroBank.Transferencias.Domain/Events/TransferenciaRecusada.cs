using EuroBank.Transferencias.Domain.Enums;
using EuroBank.Transferencias.Domain.ValueObjects;

namespace EuroBank.Transferencias.Domain.Events;

public sealed record TransferenciaRecusada(
    TransferenciaId TransferenciaId,
    Guid ContaOrigemId,
    MotivoTransferencia Motivo,
    DateTimeOffset OcorridoEm) : IEventoDeTransferencia;
