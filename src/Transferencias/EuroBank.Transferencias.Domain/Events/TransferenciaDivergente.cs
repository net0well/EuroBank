using EuroBank.Transferencias.Domain.Enums;
using EuroBank.Transferencias.Domain.ValueObjects;

namespace EuroBank.Transferencias.Domain.Events;

/// <summary>O dinheiro não fecha (Motivo diz por quê): a transferência espera a reconciliação.</summary>
public sealed record TransferenciaDivergente(
    TransferenciaId TransferenciaId,
    Guid ContaOrigemId,
    MotivoTransferencia Motivo,
    ResumoTransferencia Resumo,
    DateTimeOffset OcorridoEm) : IEventoDeTransferencia;
