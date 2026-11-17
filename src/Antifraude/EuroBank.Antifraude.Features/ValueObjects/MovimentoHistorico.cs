namespace EuroBank.Antifraude.Features.ValueObjects;

/// <summary>Uma saída ou entrada de dinheiro da conta, na moeda dela. IBAN normalizado (nulo numa entrada).</summary>
public sealed record MovimentoHistorico(DateTimeOffset Momento, decimal Valor, string? Iban = null);
