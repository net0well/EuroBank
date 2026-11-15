namespace EuroBank.Antifraude.Application.TransferObjects;

/// <summary>Uma versão na lista: status, as métricas principais no teste temporal e o limiar recomendado.</summary>
public sealed record VersaoModeloResumoDto(
    int Versao,
    string Status,
    double AucPr,
    double Precisao,
    double Recall,
    double F1,
    double LimiarRecomendado,
    bool MetaAtingida,
    DateTimeOffset TreinadaEm,
    DateTimeOffset? AtivadaEm,
    DateTimeOffset? AposentadaEm);
