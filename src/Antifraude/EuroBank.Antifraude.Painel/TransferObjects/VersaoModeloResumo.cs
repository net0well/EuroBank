namespace EuroBank.Antifraude.Painel.TransferObjects;

/// <summary>Uma versão na lista, com as métricas principais.</summary>
public sealed record VersaoModeloResumo(
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
