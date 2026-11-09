using EuroBank.Transferencias.Domain.ValueObjects;

namespace EuroBank.Transferencias.Domain.Events;

/// <summary>Compensação: pede a Contas que devolva valor e tarifa.</summary>
public sealed record EstornoSolicitado(
    TransferenciaId TransferenciaId,
    Guid ContaOrigemId,
    int Tentativa,
    DateTimeOffset OcorridoEm) : IEventoDeTransferencia;
