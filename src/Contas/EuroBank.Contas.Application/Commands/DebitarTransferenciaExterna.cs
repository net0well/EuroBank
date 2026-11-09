namespace EuroBank.Contas.Application.Commands;

/// <summary>Comando da saga de Transferencias (via eurobank.contas.comandos): debitar valor e tarifa da conta.</summary>
public sealed record DebitarTransferenciaExterna(
    Guid TransferenciaId,
    Guid ContaId,
    decimal Valor,
    decimal Tarifa,
    string IbanDestino,
    string NomeBeneficiario,
    string? Descricao);
