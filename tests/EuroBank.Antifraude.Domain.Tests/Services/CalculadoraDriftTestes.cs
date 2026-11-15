using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.Services;
using EuroBank.Antifraude.Domain.ValueObjects;

namespace EuroBank.Antifraude.Domain.Tests.Services;

/// <summary>PSI contra as faixas do treino e as faixas clássicas de nível (0,1 e 0,25).</summary>
public sealed class CalculadoraDriftTestes
{
    /// <summary>Duas faixas, metade do treino em cada: abaixo de 1 e de 1 em diante.</summary>
    private static readonly DistribuicaoFeature MetadeMetade = new("LogValor", [1], [0.5, 0.5]);

    private static List<double> Valores(int abaixo, int acima) =>
        [.. Enumerable.Repeat(0.0, abaixo), .. Enumerable.Repeat(2.0, acima)];

    [Fact]
    public void MesmaDistribuicao_PsiZero()
    {
        CalculadoraDrift.Psi(MetadeMetade, Valores(50, 50)).ShouldBe(0, tolerance: 1e-12);
    }

    [Fact]
    public void Sessenta_Quarenta_PsiPequeno()
    {
        // 0,1 × ln(0,6 / 0,5) + (-0,1) × ln(0,4 / 0,5)
        CalculadoraDrift.Psi(MetadeMetade, Valores(60, 40)).ShouldBe(0.0405465, tolerance: 1e-6);
    }

    [Fact]
    public void FaixaVazia_UsaEpsilon_PsiGrande()
    {
        // (1 - 0,5) × ln(1 / 0,5) + (0,0001 - 0,5) × ln(0,0001 / 0,5)
        CalculadoraDrift.Psi(MetadeMetade, Valores(100, 0)).ShouldBe(4.6043184, tolerance: 1e-6);
    }

    [Fact]
    public void SemValores_ArgumentException()
    {
        Should.Throw<ArgumentException>(() => CalculadoraDrift.Psi(MetadeMetade, []));
    }

    [Theory]
    [InlineData(0.0, NivelDrift.Estavel)]
    [InlineData(0.0999, NivelDrift.Estavel)]
    [InlineData(0.1, NivelDrift.Moderado)]
    [InlineData(0.2499, NivelDrift.Moderado)]
    [InlineData(0.25, NivelDrift.Significativo)]
    [InlineData(4.6, NivelDrift.Significativo)]
    public void Classificar_FaixasClassicas(double psi, NivelDrift nivel)
    {
        CalculadoraDrift.Classificar(psi, LimitesDrift.Padrao).ShouldBe(nivel);
    }
}
