namespace EuroBank.Antifraude.Painel.TransferObjects;

/// <summary>Uma operação marcada como fraude depois do fato.</summary>
public sealed record FraudeConfirmada(
    Guid Id,
    Guid OperacaoId,
    Guid AnaliseId,
    string Analista,
    string? Observacao,
    DateTimeOffset ConfirmadaEm);
