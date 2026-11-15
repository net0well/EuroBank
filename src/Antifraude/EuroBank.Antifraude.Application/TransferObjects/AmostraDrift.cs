using EuroBank.Antifraude.Features.ValueObjects;

namespace EuroBank.Antifraude.Application.TransferObjects;

/// <summary>As features das análises da janela e as probabilidades das que a versão medida pontuou.</summary>
public sealed record AmostraDrift(IReadOnlyList<VetorFeatures> Features, IReadOnlyList<double> Scores);
