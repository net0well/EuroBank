using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.ValueObjects;
using EuroBank.Shared.Domain;

namespace EuroBank.Antifraude.Domain.Events;

/// <summary>
/// A análise chegou à decisão final: na hora (motor), pelo analista, por expiração ou por cancelamento do cliente.
/// Vai para eurobank.antifraude.eventos; a saga de Transferencias reage quando espera a revisão.
/// </summary>
public sealed record AnaliseDecidida(
    AnaliseId AnaliseId,
    Guid OperacaoId,
    Guid ContaId,
    DecisaoAnalise Decisao,
    OrigemDecisao Origem,
    int ScoreFinal,
    string? Analista,
    string? Justificativa,
    DateTimeOffset OcorridoEm) : IEventoDominio;
