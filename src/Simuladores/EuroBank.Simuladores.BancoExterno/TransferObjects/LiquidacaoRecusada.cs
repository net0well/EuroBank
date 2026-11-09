namespace EuroBank.Simuladores.BancoExterno.TransferObjects;

public sealed record LiquidacaoRecusada(Guid TransferenciaId, string Motivo) : IRespostaBancoExterno;
