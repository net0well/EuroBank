namespace EuroBank.Simuladores.BancoExterno.TransferObjects;

/// <summary>Pergunta do EuroBank: esta transferência foi liquidada?</summary>
public sealed record ConsultarLiquidacao(Guid TransferenciaId);
