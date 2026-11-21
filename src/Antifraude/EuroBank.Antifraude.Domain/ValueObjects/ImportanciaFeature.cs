namespace EuroBank.Antifraude.Domain.ValueObjects;

/// <summary>
/// Quanto o modelo depende de uma feature: pelo ganho das divisões que a usam (do FastTree, de 0 a 1 em relação à
/// maior) e por permutação no teste (quanto a AUC-PR cai quando a coluna é embaralhada). A primeira diz onde o modelo
/// olhou ao treinar; a segunda, o que faz falta para acertar.
/// </summary>
public sealed record ImportanciaFeature(string Feature, double Ganho, double QuedaAucPr);
