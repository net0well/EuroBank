namespace EuroBank.Antifraude.Api.TransferObjects;

public sealed record FraudeConfirmadaRequisicao(
    Guid OperacaoId,
    string? Analista,
    string? Observacao = null);
