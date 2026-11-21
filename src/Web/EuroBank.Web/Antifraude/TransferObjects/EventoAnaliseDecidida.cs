namespace EuroBank.Web.Antifraude.TransferObjects;

/// <summary>
/// AnaliseDecidida como o painel o lê de eurobank.antifraude.eventos. Origem: Motor, Analista, Expiracao ou Cliente;
/// só as três últimas mexem na fila (a do motor nunca teve caso).
/// </summary>
public sealed record EventoAnaliseDecidida(
    Guid OperacaoId,
    string Decisao,
    string Origem,
    int ScoreFinal,
    string? Analista,
    DateTimeOffset OcorridoEm);
