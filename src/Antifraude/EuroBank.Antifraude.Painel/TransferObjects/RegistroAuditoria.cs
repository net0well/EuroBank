namespace EuroBank.Antifraude.Painel.TransferObjects;

/// <summary>Uma linha da auditoria do caso.</summary>
public sealed record RegistroAuditoria(
    int Ordem,
    DateTimeOffset Quando,
    string Quem,
    string Acao,
    string? Detalhe);
