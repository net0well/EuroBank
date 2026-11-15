namespace EuroBank.Antifraude.Domain.ValueObjects;

/// <summary>Acertos e erros do modelo no conjunto de teste, num limiar.</summary>
public sealed record MatrizConfusao(int VerdadeirosPositivos, int FalsosPositivos, int VerdadeirosNegativos, int FalsosNegativos);
