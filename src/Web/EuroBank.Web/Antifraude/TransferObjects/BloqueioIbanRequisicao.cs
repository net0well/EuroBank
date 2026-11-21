namespace EuroBank.Web.Antifraude.TransferObjects;

/// <summary>Bloquear um IBAN.</summary>
public sealed record BloqueioIbanRequisicao(
    string Iban,
    string? Motivo,
    string Analista);
