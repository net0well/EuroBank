using System.Globalization;

namespace EuroBank.Notificacoes.Worker.Templates;

/// <summary>
/// Números e datas no formato brasileiro sem depender de ICU: o projeto roda com InvariantGlobalization, então a
/// cultura pt-BR não está disponível e os separadores são definidos à mão.
/// </summary>
public static class FormatacaoPtBr
{
    private static readonly NumberFormatInfo Numeros = new()
    {
        NumberDecimalSeparator = ",",
        NumberGroupSeparator = ".",
        NumberGroupSizes = [3],
        NumberDecimalDigits = 2,
    };

    /// <summary>1234.5 e "EUR" viram "1.234,50 EUR".</summary>
    public static string Dinheiro(decimal valor, string moeda) => $"{valor.ToString("N2", Numeros)} {moeda}";

    /// <summary>1.0692 vira "1,0692" (sem zeros à direita).</summary>
    public static string Taxa(decimal taxa) => taxa.ToString("0.######", Numeros);

    /// <summary>Sempre em UTC, que é o relógio do laboratório: "13/11/2026 às 17:05 UTC".</summary>
    public static string DataHora(DateTimeOffset instante) =>
        instante.ToUniversalTime().ToString("dd/MM/yyyy 'às' HH:mm 'UTC'", CultureInfo.InvariantCulture);
}
