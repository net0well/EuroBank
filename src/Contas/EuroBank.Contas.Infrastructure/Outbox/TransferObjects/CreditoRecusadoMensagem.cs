namespace EuroBank.Contas.Infrastructure.Outbox.TransferObjects;

/// <summary>Resposta à saga de câmbio: o crédito foi recusado (ex.: conta de destino encerrada).</summary>
public sealed record CreditoRecusadoMensagem(Guid TransferenciaId, Guid ContaId, string Motivo);
