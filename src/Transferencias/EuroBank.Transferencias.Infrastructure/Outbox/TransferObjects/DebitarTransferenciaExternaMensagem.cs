namespace EuroBank.Transferencias.Infrastructure.Outbox.TransferObjects;

/// <summary>Comando para Contas (eurobank.contas.comandos): debitar valor e tarifa da conta de origem.</summary>
public sealed record DebitarTransferenciaExternaMensagem(
    Guid TransferenciaId,
    Guid ContaId,
    decimal Valor,
    decimal Tarifa,
    string IbanDestino,
    string NomeBeneficiario,
    string? Descricao);
