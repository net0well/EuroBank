using EuroBank.Antifraude.Features.ValueObjects;

namespace EuroBank.Antifraude.Treinamento.Training;

/// <summary>Um exemplo do treino: sintético (do CSV) ou real (uma análise com decisão de analista ou fraude confirmada).</summary>
public sealed record LinhaTreino(DateTimeOffset Momento, VetorFeatures Features, bool Fraude, bool Real);
