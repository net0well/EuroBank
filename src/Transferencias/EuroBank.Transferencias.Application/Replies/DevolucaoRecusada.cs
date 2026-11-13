namespace EuroBank.Transferencias.Application.Replies;

/// <summary>Resposta de Contas: o crédito da devolução foi recusado (ex.: ContaEncerrada).</summary>
public sealed record DevolucaoRecusada(Guid TransferenciaId, Guid ContaId, string Motivo);
