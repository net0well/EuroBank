using EuroBank.Antifraude.Domain.ValueObjects;

namespace EuroBank.Antifraude.Application.TransferObjects;

/// <summary>
/// Uma versão inteira, menos o arquivo: métricas com a matriz e a curva por limiar, de onde vieram os dados e a
/// distribuição do treino (a referência do drift).
/// </summary>
public sealed record VersaoModeloDetalheDto(
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
