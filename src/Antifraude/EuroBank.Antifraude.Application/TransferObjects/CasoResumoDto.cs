namespace EuroBank.Antifraude.Application.TransferObjects;

/// <summary>Linha da fila de revisão: o caso e o essencial da operação.</summary>
public sealed record CasoResumoDto(
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
