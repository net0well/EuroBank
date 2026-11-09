namespace EuroBank.Contas.Application.Commands;

/// <summary>Segundo passo da saga de câmbio: creditar o destino na moeda dele.</summary>
public sealed record CreditarTransferenciaCambio(
    Guid TransferenciaId,
    Guid ContaId,
    decimal Valor,
    string Moeda,
    string IbanOrigem,
    string NomeOrigem,
    string? Descricao,
    string MoedaOrigem,
    decimal ValorOrigem,
    decimal Taxa);
