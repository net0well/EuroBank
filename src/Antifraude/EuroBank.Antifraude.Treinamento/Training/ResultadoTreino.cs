using EuroBank.Antifraude.Domain.ValueObjects;

namespace EuroBank.Antifraude.Treinamento.Training;

/// <summary>O que um treino produz, pronto para virar uma VersaoModelo.</summary>
public sealed record ResultadoTreino(
    byte[] Arquivo,
    MetricasModelo Metricas,
    double LimiarRecomendado,
    DadosTreino Dados,
    IReadOnlyList<DistribuicaoFeature> Distribuicao);
