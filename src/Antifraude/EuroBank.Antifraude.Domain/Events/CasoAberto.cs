using EuroBank.Antifraude.Domain.ValueObjects;
using EuroBank.Shared.Domain;

namespace EuroBank.Antifraude.Domain.Events;

/// <summary>Uma operação foi para revisão humana: o caso entra na fila dos analistas até ExpiraEm.</summary>
public sealed record CasoAberto(
    CasoId CasoId,
    AnaliseId AnaliseId,
    Guid OperacaoId,
    Guid ContaId,
    int ScoreFinal,
    DateTimeOffset ExpiraEm,
    DateTimeOffset OcorridoEm) : IEventoDominio;
