namespace EuroBank.Antifraude.Domain.ValueObjects;

/// <summary>Dinheiro que entrou na conta (depósito, transferência ou câmbio recebido), na moeda dela.</summary>
public sealed record EntradaConta(DateTimeOffset Momento, decimal Valor);
