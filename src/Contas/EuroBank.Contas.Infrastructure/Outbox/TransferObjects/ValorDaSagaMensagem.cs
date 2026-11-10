namespace EuroBank.Contas.Infrastructure.Outbox.TransferObjects;

/// <summary>Resposta à saga com o valor movimentado (reconciliação e devolução), como string decimal.</summary>
public sealed record ValorDaSagaMensagem(Guid TransferenciaId, Guid ContaId, string Valor, string Moeda);
