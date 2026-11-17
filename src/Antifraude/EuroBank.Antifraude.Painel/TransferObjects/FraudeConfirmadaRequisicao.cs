namespace EuroBank.Antifraude.Painel.TransferObjects;

/// <summary>Marcar uma operação como fraude depois do fato.</summary>
public sealed record FraudeConfirmadaRequisicao(
    Guid OperacaoId,
    string Analista,
    string? Observacao);
