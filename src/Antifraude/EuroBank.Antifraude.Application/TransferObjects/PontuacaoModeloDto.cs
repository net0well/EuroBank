namespace EuroBank.Antifraude.Application.TransferObjects;

/// <summary>
/// O que uma versão do modelo disse: a probabilidade, os pontos (do challenger: o que somaria) e a explicação, com as
/// features que mais empurraram o score primeiro.
/// </summary>
public sealed record PontuacaoModeloDto(int Versao, double Probabilidade, int Pontos, IReadOnlyList<ContribuicaoFeatureDto> Contribuicoes);
