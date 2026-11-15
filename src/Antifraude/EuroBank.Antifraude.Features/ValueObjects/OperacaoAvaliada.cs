namespace EuroBank.Antifraude.Features.ValueObjects;

/// <summary>
/// A operação vista pelas features: valor na moeda da conta, IBAN de destino normalizado (nulo num saque) e se o país
/// dele está na lista de risco (quem chama resolve a lista).
/// </summary>
public sealed record OperacaoAvaliada(DateTimeOffset Momento, decimal Valor, string? IbanDestino, bool PaisDeRisco);
