namespace EuroBank.Antifraude.Domain.ValueObjects;

/// <summary>
/// A avaliação de uma versão no conjunto de teste temporal (os dias mais recentes, que o treino não viu). AUC-PR é a
/// métrica principal: com fraude rara, a acurácia não diz nada. Precisão, recall, F1 e a matriz são no limiar
/// recomendado; a curva mostra os outros limiares.
/// </summary>
public sealed record MetricasModelo(
    double AucPr,
    double Precisao,
    double Recall,
    double F1,
    MatrizConfusao Matriz,
    IReadOnlyList<PontoCurva> Curva);
