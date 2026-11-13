namespace EuroBank.Transferencias.Application.Replies;

/// <summary>Resposta de Contas: valor e tarifa debitados de novo (a conta pode ter ficado negativa).</summary>
public sealed record ReconciliacaoRealizada(Guid TransferenciaId, Guid ContaId, string Valor, string Moeda);
