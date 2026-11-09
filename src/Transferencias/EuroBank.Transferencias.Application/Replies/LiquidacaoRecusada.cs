namespace EuroBank.Transferencias.Application.Replies;

public sealed record LiquidacaoRecusada(Guid TransferenciaId, string Motivo);
