namespace EuroBank.Web.Antifraude.TransferObjects;

/// <summary>Um caso na fila.</summary>
public sealed record CasoResumo(
    Guid Id,
    Guid AnaliseId,
    Guid OperacaoId,
    Guid ContaId,
    string TipoOperacao,
    decimal Valor,
    string Moeda,
    string? IbanDestino,
    int ScoreFinal,
    string Estado,
    string? Analista,
    DateTimeOffset AbertoEm,
    DateTimeOffset ExpiraEm,
    DateTimeOffset? DecididoEm);
