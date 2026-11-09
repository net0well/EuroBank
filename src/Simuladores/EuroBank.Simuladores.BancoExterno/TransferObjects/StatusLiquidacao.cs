namespace EuroBank.Simuladores.BancoExterno.TransferObjects;

/// <summary>Resposta a uma consulta: Status é Liquidada, NaoLiquidada ou Recusada.</summary>
public sealed record StatusLiquidacao(Guid TransferenciaId, string Status) : IRespostaBancoExterno;
