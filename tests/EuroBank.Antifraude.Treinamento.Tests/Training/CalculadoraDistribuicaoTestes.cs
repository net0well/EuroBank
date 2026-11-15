using EuroBank.Antifraude.Treinamento.Training;

namespace EuroBank.Antifraude.Treinamento.Tests.Training;

/// <summary>A referência do drift: decis do treino e a fração em cada faixa.</summary>
public sealed class CalculadoraDistribuicaoTestes
{
    [Fact]
    public void ValoresContinuos_DezFaixasComDezPorCento()
    {
        var valores = Enumerable.Range(1, 100).Select(valor => (double)valor).ToList();

        var distribuicao = CalculadoraDistribuicao.Calcular("LogValor", valores);

        distribuicao.Nome.ShouldBe("LogValor");
        distribuicao.Limites.ShouldBe([11, 21, 31, 41, 51, 61, 71, 81, 91]);
        distribuicao.Proporcoes.Count.ShouldBe(10);
        distribuicao.Proporcoes.ShouldAllBe(proporcao => Math.Abs(proporcao - 0.1) < 1e-9);
    }

    [Fact]
    public void FeatureZeroOuUm_LimitesRepetidosViramUmSo()
    {
        var valores = Enumerable.Repeat(0.0, 90).Concat(Enumerable.Repeat(1.0, 10)).ToList();

        var distribuicao = CalculadoraDistribuicao.Calcular("BeneficiarioNovo", valores);

        distribuicao.Limites.ShouldBe([0, 1]);
        distribuicao.Proporcoes.ShouldBe([0, 0.9, 0.1]);
        distribuicao.Faixa(-1).ShouldBe(0);
        distribuicao.Faixa(0).ShouldBe(1);
        distribuicao.Faixa(1).ShouldBe(2);
    }
}
