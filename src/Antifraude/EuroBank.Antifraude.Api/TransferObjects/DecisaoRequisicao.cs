namespace EuroBank.Antifraude.Api.TransferObjects;

/// <summary>Decisao: Aprovar ou Recusar. BloquearIban só ao recusar.</summary>
public sealed record DecisaoRequisicao(
    string? Analista,
    string? Decisao,
    string? Justificativa,
    bool BloquearIban = false);
