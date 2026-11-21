namespace EuroBank.Web.Antifraude.TransferObjects;

/// <summary>Uma versão inteira, menos o arquivo.</summary>
public sealed record VersaoModeloDetalhe(
    int Versao,
    string Status,
    double LimiarRecomendado,
    MetricasModelo Metricas,
    DadosTreino Dados,
    IReadOnlyList<DistribuicaoFeature> Distribuicao,
    int TamanhoArquivo,
    DateTimeOffset TreinadaEm,
    DateTimeOffset? AtivadaEm,
    DateTimeOffset? AposentadaEm);
