namespace EuroBank.Contas.Application.Commands;

/// <summary>Devolução pelo banco externo: creditar o valor (sem a tarifa) de uma transferência concluída.</summary>
public sealed record CreditarDevolucao(Guid TransferenciaId, Guid ContaId);
