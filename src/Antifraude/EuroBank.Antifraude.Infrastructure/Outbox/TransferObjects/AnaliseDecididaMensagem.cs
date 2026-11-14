namespace EuroBank.Antifraude.Infrastructure.Outbox.TransferObjects;

/// <summary>
/// Evento AnaliseDecidida (eurobank.antifraude.eventos, chave = id da operação). Decisao: Aprovada, Recusada,
/// Expirada ou Cancelada; Origem: Motor, Analista, Expiracao ou Cliente.
/// </summary>
public sealed record AnaliseDecididaMensagem(
    Guid AnaliseId,
    Guid OperacaoId,
    Guid ContaId,
    string Decisao,
    string Origem,
    int ScoreFinal,
    string? Analista,
    string? Justificativa,
    DateTimeOffset OcorridoEm);
