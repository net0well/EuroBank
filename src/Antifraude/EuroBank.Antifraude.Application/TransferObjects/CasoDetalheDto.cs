namespace EuroBank.Antifraude.Application.TransferObjects;

/// <summary>O caso com a análise inteira (contexto, sinais de cada regra) e a auditoria.</summary>
public sealed record CasoDetalheDto(
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
    AnaliseDetalheDto Analise,
    IReadOnlyList<RegistroAuditoriaDto> Auditoria);
