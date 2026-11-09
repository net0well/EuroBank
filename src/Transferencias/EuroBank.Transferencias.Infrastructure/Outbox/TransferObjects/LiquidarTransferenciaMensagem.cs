namespace EuroBank.Transferencias.Infrastructure.Outbox.TransferObjects;

/// <summary>Comando para o banco externo (eurobank.banco-externo.comandos).</summary>
public sealed record LiquidarTransferenciaMensagem(
    Guid TransferenciaId,
    string IbanDestino,
    string Bic,
    string NomeBeneficiario,
    decimal Valor,
    string Moeda);
