using EuroBank.Transferencias.Domain.Enums;
using EuroBank.Transferencias.Domain.ValueObjects;

namespace EuroBank.Transferencias.Domain.Events;

public sealed record TransferenciaCancelada(
    TransferenciaId TransferenciaId,
    Guid ContaOrigemId,
    MotivoTransferencia Motivo,
    ResumoTransferencia Resumo,
    DateTimeOffset OcorridoEm) : IEventoDeTransferencia;
