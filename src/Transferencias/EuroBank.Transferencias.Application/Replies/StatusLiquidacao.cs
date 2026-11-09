namespace EuroBank.Transferencias.Application.Replies;

/// <summary>Resposta do banco externo a uma consulta: Liquidada, NaoLiquidada ou Recusada.</summary>
public sealed record StatusLiquidacao(Guid TransferenciaId, string Status);
