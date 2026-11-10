namespace EuroBank.Contas.Application.Commands;

/// <summary>Reconciliação pedida pela saga: debitar de novo valor e tarifa de uma transferência estornada.</summary>
public sealed record DebitarReconciliacao(Guid TransferenciaId, Guid ContaId);
