namespace EuroBank.Antifraude.Domain.ValueObjects;

/// <summary>
/// Um ponto da curva de aprendizado: a AUC-PR com só as primeiras árvores do ensemble, no treino e no teste. Quando o
/// teste para de subir e o treino continua, as árvores a mais só decoram o treino.
/// </summary>
public sealed record PontoAprendizado(int Arvores, double AucPrTreino, double AucPrTeste);
