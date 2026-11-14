namespace EuroBank.Antifraude.Domain.Entities;

/// <summary>Uma linha da auditoria do caso: quem fez o quê, quando. Ordem preserva a sequência (1, 2, 3...).</summary>
public sealed class RegistroAuditoria
{
    private RegistroAuditoria()
    {
        // Usado pelo EF Core.
    }

    internal RegistroAuditoria(int ordem, DateTimeOffset quando, string quem, string acao, string? detalhe)
    {
        Ordem = ordem;
        Quando = quando;
        Quem = quem;
        Acao = acao;
        Detalhe = detalhe;
    }

    public int Ordem { get; private set; }

    public DateTimeOffset Quando { get; private set; }

    public string Quem { get; private set; } = string.Empty;

    public string Acao { get; private set; } = string.Empty;

    public string? Detalhe { get; private set; }
}
