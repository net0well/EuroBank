namespace EuroBank.Web.Antifraude.TransferObjects;

/// <summary>Quanto uma feature empurrou o score (positivo: na direção de fraude).</summary>
public sealed record ContribuicaoFeature(
    string Feature,
    double Valor);
