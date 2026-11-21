namespace EuroBank.Antifraude.Domain.ValueObjects;

/// <summary>
/// O que um treino ensinou, além das métricas da versão: a curva de aprendizado, a importância das features, a
/// distribuição dos scores, a calibração, o recall por padrão de fraude e a comparação com a versão ativa.
/// </summary>
public sealed record RelatorioTreino(
    DadosTreino Dados,
    MetricasModelo Metricas,
    double LimiarRecomendado,
    IReadOnlyList<PontoAprendizado> CurvaAprendizado,
    IReadOnlyList<ImportanciaFeature> Importancias,
    IReadOnlyList<FaixaScore> DistribuicaoScores,
    IReadOnlyList<FaixaCalibracao> Calibracao,
    IReadOnlyList<RecallPadrao> RecallPorPadrao,
    ComparacaoTreino? Ativa);
