using EuroBank.Antifraude.Domain.Exceptions;

namespace EuroBank.Antifraude.Domain.Entities;

/// <summary>País (código ISO de duas letras, o mesmo do começo do IBAN) na lista de países de risco.</summary>
public sealed class PaisRisco
{
    private PaisRisco()
    {
        // Usado pelo EF Core.
    }

    public string Codigo { get; private set; } = string.Empty;

    public DateTimeOffset IncluidoEm { get; private set; }

    public static PaisRisco Criar(string codigo, DateTimeOffset agora)
    {
        var normalizado = codigo.Trim().ToUpperInvariant();
        if (normalizado.Length != 2 || !normalizado.All(char.IsAsciiLetterUpper))
        {
            throw new ExcecaoPaisInvalido(codigo);
        }

        return new PaisRisco { Codigo = normalizado, IncluidoEm = agora };
    }
}
