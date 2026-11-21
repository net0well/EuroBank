namespace EuroBank.Antifraude.Domain.ValueObjects;

/// <summary>
/// Calibração numa faixa de probabilidade prevista: a média do que o modelo disse e a fração de fraudes que de fato
/// havia. Num modelo calibrado, as duas andam juntas (a diagonal).
/// </summary>
public sealed record FaixaCalibracao(double De, double Ate, int Operacoes, double ProbabilidadeMedia, double TaxaObservada);
