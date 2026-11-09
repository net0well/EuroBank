namespace EuroBank.Transferencias.Application.Replies;

/// <summary>Resposta de Contas: nada foi debitado (Motivo: SaldoInsuficiente, ContaBloqueada, ContaEncerrada...).</summary>
public sealed record DebitoRecusado(Guid TransferenciaId, Guid ContaId, string Motivo);
