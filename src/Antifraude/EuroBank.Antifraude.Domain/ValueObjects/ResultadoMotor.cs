using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Features.ValueObjects;

namespace EuroBank.Antifraude.Domain.ValueObjects;

/// <summary>
/// A decisão do motor, o score (0 a 100), o sinal de cada regra avaliada, a configuração usada e as features da
/// operação (as mesmas que o modelo recebe; nulas só em análises montadas à mão nos testes).
/// </summary>
public sealed record ResultadoMotor(
    DecisaoAnalise Decisao,
    int ScoreFinal,
    IReadOnlyList<SinalRegra> Sinais,
    int VersaoConfiguracao,
    VetorFeatures? Features = null);
