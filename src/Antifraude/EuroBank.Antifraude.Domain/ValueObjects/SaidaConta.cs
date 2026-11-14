namespace EuroBank.Antifraude.Domain.ValueObjects;

/// <summary>Uma transferência que saiu da conta (valor na moeda da conta, sem a tarifa).</summary>
public sealed record SaidaConta(DateTimeOffset Momento, decimal Valor, string? IbanDestino);
