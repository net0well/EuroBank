namespace EuroBank.Transferencias.Application.Replies;

/// <summary>Resposta de Contas: a reconciliação foi recusada (ex.: ContaEncerrada).</summary>
public sealed record ReconciliacaoRecusada(Guid TransferenciaId, Guid ContaId, string Motivo);
