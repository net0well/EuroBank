namespace EuroBank.Contas.Application.Commands;

/// <summary>Primeiro passo da saga de câmbio entre contas EuroBank: debitar a origem na moeda dela.</summary>
public sealed record DebitarTransferenciaCambio(
    Guid TransferenciaId,
    Guid ContaId,
    decimal Valor,
    decimal Tarifa,
    string Moeda,
    string IbanDestino,
    string NomeDestino,
    string? Descricao,
    string MoedaDestino,
    decimal ValorDestino,
    decimal Taxa);
