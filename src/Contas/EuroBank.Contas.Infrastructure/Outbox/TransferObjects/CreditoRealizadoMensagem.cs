namespace EuroBank.Contas.Infrastructure.Outbox.TransferObjects;

/// <summary>Resposta à saga de câmbio (eurobank.contas.respostas): o destino foi creditado.</summary>
public sealed record CreditoRealizadoMensagem(Guid TransferenciaId, Guid ContaId, string Valor, string Moeda);
