using EuroBank.Antifraude.Domain.ValueObjects;

namespace EuroBank.Antifraude.Application.TransferObjects;

public sealed record ReferenciaDrift(int Versao, IReadOnlyList<DistribuicaoFeature> Distribuicao);
