namespace EuroBank.Antifraude.Painel.TransferObjects;

/// <summary>Os números das últimas 24 horas e o drift do modelo ativo.</summary>
public sealed record VisaoGeral(
    DateTimeOffset Desde,
    int Analises,
    IReadOnlyDictionary<string, int> PorDecisao,
    decimal TaxaRevisao,
    decimal TaxaRecusa,
    int CasosPendentes,
    int CasosDecididos,
    int CasosExpirados,
    double? MinutosMediosAteDecisao,
    IReadOnlyList<RegraDisparada> RegrasMaisDisparadas,
    Drift Drift);
