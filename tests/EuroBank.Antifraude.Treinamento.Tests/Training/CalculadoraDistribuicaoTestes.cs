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
        distribuicao.Limites.ShouldBe([10, 20, 30, 40, 50, 60, 70, 80, 90]);
        distribuicao.Proporcoes.Count.ShouldBe(10);
        distribuicao.Proporcoes.ShouldAllBe(proporcao => Math.Abs(proporcao - 0.1) < 1e-9);
    }

    [Fact]
    public void FeatureQuaseSempreZero_OLimiteZeroSeparaOZeroDoResto()
    {
        // 97% de zeros: todos os decis caem em 0 e sobra um limite só, que ainda separa o zero dos positivos.
        var valores = Enumerable.Repeat(0.0, 97).Concat([1.0, 2.0, 3.0]).ToList();

        var distribuicao = CalculadoraDistribuicao.Calcular("SaidasUltimaHora", valores);

        distribuicao.Limites.ShouldBe([0]);
        distribuicao.Proporcoes.ShouldBe([0.97, 0.03], tolerance: 1e-9);
        distribuicao.Faixa(0).ShouldBe(0);
        distribuicao.Faixa(1).ShouldBe(1);
        distribuicao.Faixa(5).ShouldBe(1);
    }

    [Fact]
    public void FeatureZeroOuUm_DuasFaixas()
    {
        var valores = Enumerable.Repeat(0.0, 80).Concat(Enumerable.Repeat(1.0, 20)).ToList();

        var distribuicao = CalculadoraDistribuicao.Calcular("BeneficiarioNovo", valores);

        distribuicao.Limites.ShouldBe([0, 1]);
        distribuicao.Proporcoes.ShouldBe([0.8, 0.2, 0], tolerance: 1e-9);
    }
}
