using System.Globalization;
using System.Text.RegularExpressions;
using EuroBank.Contas.Domain.Exceptions;

namespace EuroBank.Contas.Domain.ValueObjects;

/// <summary>
/// IBAN (ISO 13616). Os IBANs gerados pelo EuroBank são fictícios: país PT, código de banco EURO e um
/// número sequencial de 13 dígitos, com dígito verificador módulo 97 válido.
/// </summary>
public sealed partial record Iban
{
    private const string Pais = "PT";
    private const string CodigoBanco = "EURO";

    private Iban(string valor) => Valor = valor;

    public string Valor { get; }

    /// <summary>Gera o IBAN do EuroBank para um número sequencial de conta.</summary>
    public static Iban Gerar(long sequencial)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sequencial);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(sequencial, 9_999_999_999_999L);

        var bban = CodigoBanco + sequencial.ToString("D13", CultureInfo.InvariantCulture);
        var digitos = 98 - Modulo97(bban + Pais + "00");
        return new Iban($"{Pais}{digitos:D2}{bban}");
    }

    /// <summary>Lê um IBAN informado pelo cliente (aceita espaços e minúsculas).</summary>
    public static Iban Criar(string valor)
    {
        var normalizado = Normalizar(valor);
        if (!EhValidoNormalizado(normalizado))
        {
            throw new ExcecaoIbanInvalido(valor);
        }

        return new Iban(normalizado);
    }

    public static bool EhValido(string valor) => EhValidoNormalizado(Normalizar(valor));

    public override string ToString() => Valor;

    private static string Normalizar(string? valor) =>
        (valor ?? string.Empty).Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();

    private static bool EhValidoNormalizado(string valor) =>
        Formato().IsMatch(valor) && Modulo97(valor[4..] + valor[..4]) == 1;

    /// <summary>Converte letras em números (A=10 ... Z=35) e calcula o resto por 97 dígito a dígito.</summary>
    private static int Modulo97(string texto)
    {
        var resto = 0;
        foreach (var caractere in texto)
        {
            if (char.IsDigit(caractere))
            {
                resto = ((resto * 10) + (caractere - '0')) % 97;
            }
            else
            {
                resto = ((resto * 100) + (caractere - 'A' + 10)) % 97;
            }
        }

        return resto;
    }

    [GeneratedRegex("^[A-Z]{2}[0-9]{2}[A-Z0-9]{11,30}$")]
    private static partial Regex Formato();
}
