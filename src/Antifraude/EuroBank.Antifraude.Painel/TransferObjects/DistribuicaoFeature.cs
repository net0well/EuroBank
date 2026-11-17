namespace EuroBank.Antifraude.Painel.TransferObjects;

/// <summary>A distribuição de uma feature no treino (a referência do drift).</summary>
public sealed record DistribuicaoFeature(
    string Nome,
    IReadOnlyList<double> Limites,
    IReadOnlyList<double> Proporcoes);
