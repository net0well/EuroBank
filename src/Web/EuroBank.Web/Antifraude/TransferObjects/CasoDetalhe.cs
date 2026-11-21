namespace EuroBank.Web.Antifraude.TransferObjects;

/// <summary>O caso com a análise inteira e a auditoria.</summary>
public sealed record CasoDetalhe(
    Guid Id,
    string Estado,
    string? Analista,
    DateTimeOffset AbertoEm,
    DateTimeOffset ExpiraEm,
    DateTimeOffset? AssumidoEm,
    DateTimeOffset? DecididoEm,
    string? Justificativa,
    bool BloquearIban,
    int Versao,
    AnaliseDetalhe Analise,
    IReadOnlyList<RegistroAuditoria> Auditoria);
