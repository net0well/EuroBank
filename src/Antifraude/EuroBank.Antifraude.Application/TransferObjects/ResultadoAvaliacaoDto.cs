using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.ValueObjects;

namespace EuroBank.Antifraude.Application.TransferObjects;

/// <summary>
/// O resultado de uma avaliação, gravada ou simulada. AnaliseId nulo numa simulação; CasoId só quando a operação foi
/// para revisão; DecididaEm nulo enquanto a revisão não termina; Modelo e Challenger nulos sem versão carregada.
/// </summary>
public sealed record ResultadoAvaliacaoDto(
    Guid? AnaliseId,
    Guid OperacaoId,
    DecisaoAnalise Decisao,
    int ScoreFinal,
    Guid? CasoId,
    IReadOnlyList<SinalRegra> Sinais,
    int VersaoConfiguracao,
    DateTimeOffset AvaliadaEm,
    DateTimeOffset? DecididaEm,
    bool Simulacao,
    PontuacaoModelo? Modelo = null,
    PontuacaoModelo? Challenger = null);
