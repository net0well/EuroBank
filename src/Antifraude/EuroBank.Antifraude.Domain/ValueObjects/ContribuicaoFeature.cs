namespace EuroBank.Antifraude.Domain.ValueObjects;

/// <summary>Quanto uma feature empurrou o score do modelo nesta operação (positivo: na direção de fraude).</summary>
public sealed record ContribuicaoFeature(string Feature, float Valor);
