namespace EuroBank.Antifraude.Api.TransferObjects;

/// <summary>Código ISO de duas letras (o começo do IBAN).</summary>
public sealed record PaisRiscoRequisicao(
    string? Codigo);
