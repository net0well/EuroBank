namespace EuroBank.Antifraude.Application.Commands;

public sealed record ConfirmarFraude(Guid OperacaoId, string Analista, string? Observacao);
