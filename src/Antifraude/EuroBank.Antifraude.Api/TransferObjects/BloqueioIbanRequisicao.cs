namespace EuroBank.Antifraude.Api.TransferObjects;

public sealed record BloqueioIbanRequisicao(
    string? Iban,
    string? Motivo,
    string? Analista);
