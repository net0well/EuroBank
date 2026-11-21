namespace EuroBank.Web.Antifraude.TransferObjects;

/// <summary>A probabilidade, os pontos e a explicação de uma versão do modelo.</summary>
public sealed record PontuacaoModelo(
    int Versao,
    double Probabilidade,
    int Pontos,
    IReadOnlyList<ContribuicaoFeature> Contribuicoes);
