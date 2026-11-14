using EuroBank.Antifraude.Domain.Enums;

namespace EuroBank.Antifraude.Domain.ValueObjects;

/// <summary>A decisão do motor, o score (0 a 100), o sinal de cada regra avaliada e a configuração usada.</summary>
public sealed record ResultadoMotor(
    DecisaoAnalise Decisao,
    int ScoreFinal,
    IReadOnlyList<SinalRegra> Sinais,
    int VersaoConfiguracao);
