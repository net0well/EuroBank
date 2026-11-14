namespace EuroBank.Antifraude.Domain.ValueObjects;

/// <summary>Uma linha da auditoria do caso: quem fez o quê, quando (Ordem preserva a sequência).</summary>
public sealed record RegistroAuditoria(int Ordem, DateTimeOffset Quando, string Quem, string Acao, string? Detalhe);
