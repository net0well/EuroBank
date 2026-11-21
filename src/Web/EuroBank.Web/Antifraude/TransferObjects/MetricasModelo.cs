namespace EuroBank.Web.Antifraude.TransferObjects;

/// <summary>A avaliação no teste temporal: no limiar recomendado, e a curva nos outros.</summary>
public sealed record MetricasModelo(
    double AucPr,
    double Precisao,
    double Recall,
    double F1,
    MatrizConfusao Matriz,
    IReadOnlyList<PontoCurva> Curva);
