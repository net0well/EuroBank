namespace EuroBank.Transferencias.Application.Replies;

/// <summary>Resposta de Contas ao câmbio interno: o crédito foi recusado (ex.: ContaEncerrada).</summary>
public sealed record CreditoRecusado(Guid TransferenciaId, Guid ContaId, string Motivo);
