using EuroBank.Transferencias.Domain.ValueObjects;

namespace EuroBank.Transferencias.Domain.Events;

/// <summary>Reconciliação de uma Divergente: pede a Contas que debite de novo valor e tarifa, mesmo sem saldo.</summary>
public sealed record ReconciliacaoSolicitada(
    TransferenciaId TransferenciaId,
    Guid ContaOrigemId,
    int Tentativa,
    DateTimeOffset OcorridoEm) : IEventoDeTransferencia;
