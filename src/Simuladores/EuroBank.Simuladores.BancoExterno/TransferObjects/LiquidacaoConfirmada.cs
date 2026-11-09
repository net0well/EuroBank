namespace EuroBank.Simuladores.BancoExterno.TransferObjects;

public sealed record LiquidacaoConfirmada(Guid TransferenciaId, DateTimeOffset ConfirmadaEm) : IRespostaBancoExterno;
