namespace EuroBank.Antifraude.Painel.TransferObjects;

/// <summary>Acertos e erros no limiar recomendado.</summary>
public sealed record MatrizConfusao(
    int VerdadeirosPositivos,
    int FalsosPositivos,
    int VerdadeirosNegativos,
    int FalsosNegativos);
