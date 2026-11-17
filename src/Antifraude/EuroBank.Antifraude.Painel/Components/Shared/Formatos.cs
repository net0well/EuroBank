using System.Globalization;
using MudBlazor;

namespace EuroBank.Antifraude.Painel.Components.Shared;

/// <summary>
/// Como o painel mostra números, datas e estados. Cultura invariante, como o resto do laboratório (o container não
/// tem as culturas instaladas): ponto decimal e datas em UTC.
/// </summary>
public static class Formatos
{
    public static string Valor(decimal valor, string moeda) => string.Create(CultureInfo.InvariantCulture, $"{valor:N2} {moeda}");

    public static string Data(DateTimeOffset? quando) =>
        quando is { } valor ? valor.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " UTC" : "-";

    public static string Decimal(double valor, int casas = 3) =>
        valor.ToString("F" + casas.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

    public static string Percentual(double fracao) => (fracao * 100).ToString("F1", CultureInfo.InvariantCulture) + "%";

    public static string Percentual(decimal fracao) => Percentual((double)fracao);

    /// <summary>A decisão de uma análise (Aprovada, EmRevisao, Recusada, Expirada, Cancelada).</summary>
    public static Color CorDecisao(string decisao) => decisao switch
    {
        "Aprovada" or "Aprovado" => Color.Success,
        "Recusada" or "Recusado" => Color.Error,
        "EmRevisao" or "Aberto" or "EmAnalise" => Color.Warning,
        _ => Color.Default,
    };

    /// <summary>O status de uma versão do modelo.</summary>
    public static Color CorStatusModelo(string status) => status switch
    {
        "Ativa" => Color.Success,
        "Challenger" => Color.Info,
        "Registrada" => Color.Default,
        _ => Color.Dark,
    };

    /// <summary>O nível do drift (Estavel, Moderado, Significativo).</summary>
    public static Color CorNivelDrift(string? nivel) => nivel switch
    {
        "Estavel" => Color.Success,
        "Moderado" => Color.Warning,
        "Significativo" => Color.Error,
        _ => Color.Default,
    };

    /// <summary>A decisão no formato do proto (DECISAO_APROVADA) no formato da REST (Aprovada).</summary>
    public static string DecisaoDoProto(string decisao) => decisao switch
    {
        "DECISAO_APROVADA" => "Aprovada",
        "DECISAO_EM_REVISAO" => "EmRevisao",
        "DECISAO_RECUSADA" => "Recusada",
        _ => decisao,
    };
}
