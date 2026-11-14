namespace EuroBank.Antifraude.Application.TransferObjects;

public sealed record FraudeConfirmadaDto(
    Guid Id,
    Guid OperacaoId,
    Guid AnaliseId,
    string Analista,
    string? Observacao,
    DateTimeOffset ConfirmadaEm);
