using EuroBank.Antifraude.Treinamento.Enums;

namespace EuroBank.Antifraude.Treinamento.Generation;

/// <summary>
/// O jeito de uma conta simulada: salário e dia do salário, quantas transferências faz por dia, o valor típico, a hora
/// de pico e os beneficiários habituais (a lista cresce quando um beneficiário novo vira habitual). Mula é uma conta
/// aberta só para repassar dinheiro.
/// </summary>
public sealed record ClienteSimulado(
    Guid ContaId,
    DateTimeOffset AbertaEm,
    decimal Salario,
    int DiaSalario,
    double TaxaDiaria,
    decimal ValorTipico,
    int HoraPico,
    List<string> Beneficiarios,
    bool Mula)
{
    public List<(PadraoFraude Padrao, DateTimeOffset Momento)> Episodios { get; } = [];
}
