namespace EuroBank.Antifraude.Application.TransferObjects;

/// <summary>Números das últimas 24 horas (Desde). Taxas de 0 a 1; casos pendentes contam todos os abertos agora.</summary>
public sealed record VisaoGeralDto(
    DateTimeOffset Desde,
    int Analises,
    IReadOnlyDictionary<string, int> PorDecisao,
    decimal TaxaRevisao,
    decimal TaxaRecusa,
    int CasosPendentes,
    int CasosDecididos,
    int CasosExpirados,
    double? MinutosMediosAteDecisao,
    IReadOnlyList<RegraDisparadaDto> RegrasMaisDisparadas);
