namespace EuroBank.Antifraude.Domain.Policies;

/// <summary>IBANs comparados sem espaços e em maiúsculas; o país são as duas primeiras letras.</summary>
public static class NormalizacaoIban
{
    public static string? Normalizar(string? iban) =>
        string.IsNullOrWhiteSpace(iban) ? null : iban.Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();

    public static string? Pais(string? iban) =>
        Normalizar(iban) is { Length: >= 2 } normalizado && char.IsLetter(normalizado[0]) && char.IsLetter(normalizado[1])
            ? normalizado[..2]
            : null;
}
