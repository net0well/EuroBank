namespace EuroBank.Simuladores.BancoExterno.TransferObjects;

/// <summary>Comando recebido do EuroBank em eurobank.banco-externo.comandos.</summary>
public sealed record LiquidarTransferencia(
    Guid TransferenciaId, string IbanDestino, string? Bic, string NomeBeneficiario, decimal Valor, string Moeda);
