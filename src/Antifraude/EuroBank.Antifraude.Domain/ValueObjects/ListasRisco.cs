using EuroBank.Antifraude.Domain.Policies;

namespace EuroBank.Antifraude.Domain.ValueObjects;

/// <summary>IBANs bloqueados e países de risco (códigos de duas letras), mantidos pelos analistas.</summary>
public sealed record ListasRisco(IReadOnlySet<string> IbansBloqueados, IReadOnlySet<string> PaisesRisco)
{
    public static ListasRisco Vazias { get; } = new(new HashSet<string>(), new HashSet<string>());

    public bool EstaBloqueado(string? iban) =>
        NormalizacaoIban.Normalizar(iban) is { } normalizado
        && IbansBloqueados.Any(bloqueado => NormalizacaoIban.Normalizar(bloqueado) == normalizado);

    public bool EhPaisDeRisco(string? pais) =>
        pais is not null && PaisesRisco.Any(risco => string.Equals(risco, pais, StringComparison.OrdinalIgnoreCase));
}
