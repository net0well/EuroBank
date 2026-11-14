using System.Globalization;

namespace EuroBank.Antifraude.Domain.Rules;

/// <summary>Números nos motivos das regras, sempre no mesmo formato ("1234.50").</summary>
internal static class Formatacao
{
    public static string Valor(decimal valor) => valor.ToString("0.00", CultureInfo.InvariantCulture);

    public static string Numero(decimal numero) => numero.ToString("0.##", CultureInfo.InvariantCulture);
}
