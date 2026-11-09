namespace EuroBank.Contas.Application.Commands;

/// <summary>
/// Comando da saga de Transferencias (via eurobank.contas.comandos): debitar valor e tarifa da conta. Moeda é a
/// moeda esperada da conta; com câmbio, MoedaDestino, ValorDestino e Taxa dizem o que o destino recebe.
/// </summary>
public sealed record DebitarTransferenciaExterna(
    Guid TransferenciaId,
    Guid ContaId,
    decimal Valor,
    decimal Tarifa,
    string IbanDestino,
    string NomeBeneficiario,
    string? Descricao,
    string? Moeda = null,
    string? MoedaDestino = null,
    decimal? ValorDestino = null,
    decimal? Taxa = null);
