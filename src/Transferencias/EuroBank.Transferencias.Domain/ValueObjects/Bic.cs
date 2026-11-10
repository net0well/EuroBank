using System.Text.RegularExpressions;
using EuroBank.Transferencias.Domain.Exceptions;

namespace EuroBank.Transferencias.Domain.ValueObjects;

/// <summary>BIC/SWIFT (ISO 9362): banco (4 letras), país (2 letras), localidade (2) e agência opcional (3).</summary>
public static partial class Bic
{
    /// <exception cref="ExcecaoBicObrigatorio">BIC vazio.</exception>
    /// <exception cref="ExcecaoBicInvalido">BIC fora do formato.</exception>
    public static string Normalizar(string? bic)
    {
        if (string.IsNullOrWhiteSpace(bic))
        {
            throw new ExcecaoBicObrigatorio();
        }

        var normalizado = bic.Trim().ToUpperInvariant();
        return Formato().IsMatch(normalizado) ? normalizado : throw new ExcecaoBicInvalido(bic);
    }

    [GeneratedRegex("^[A-Z]{4}[A-Z]{2}[A-Z0-9]{2}([A-Z0-9]{3})?$")]
    private static partial Regex Formato();
}
