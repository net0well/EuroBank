using EuroBank.Antifraude.Treinamento.Training;

namespace EuroBank.Antifraude.Treinamento.Tests.Training;

/// <summary>O limiar recomendado: o mais alto que alcança o recall da meta; sem nenhum, o de melhor F1.</summary>
public sealed class AvaliadorLimiaresTestes
{
    private static List<(bool Fraude, float Probabilidade)> Previsoes(IEnumerable<float> fraudes, IEnumerable<float> normais) =>
        [.. fraudes.Select(probabilidade => (true, probabilidade)), .. normais.Select(probabilidade => (false, probabilidade))];

    [Fact]
    public void SeparacaoPerfeita_LimiarMaisAltoComRecallDaMeta_MetaAtingida()
    {
        var previsoes = Previsoes(Enumerable.Repeat(0.95f, 4), Enumerable.Repeat(0.05f, 96));

        var escolha = AvaliadorLimiares.Recomendar(previsoes, metaRecall: 0.9, metaTaxaSinalizada: 0.05);

        escolha.Limiar.ShouldBe(0.95, tolerance: 1e-9);
        escolha.MetaAtingida.ShouldBeTrue();
        escolha.Matriz.ShouldBe(new(VerdadeirosPositivos: 4, FalsosPositivos: 0, VerdadeirosNegativos: 96, FalsosNegativos: 0));
        escolha.Precisao.ShouldBe(1);
        escolha.Recall.ShouldBe(1);
        escolha.F1.ShouldBe(1);
    }

    [Fact]
    public void RecallDaMeta_SinalizandoDemais_MetaNaoAtingida()
    {
        // Para pegar as fraudes, o limiar precisa ficar abaixo de 0,5: 30% das operações acima dele.
        var previsoes = Previsoes(Enumerable.Repeat(0.45f, 10), [.. Enumerable.Repeat(0.4f, 20), .. Enumerable.Repeat(0.1f, 70)]);

        var escolha = AvaliadorLimiares.Recomendar(previsoes, metaRecall: 0.9, metaTaxaSinalizada: 0.05);

        escolha.Limiar.ShouldBe(0.45, tolerance: 1e-9);
        escolha.Recall.ShouldBe(1);
        escolha.MetaAtingida.ShouldBeFalse();
    }

    [Fact]
    public void NenhumLimiarComORecallDaMeta_FicaODeMelhorF1()
    {
        // Uma fraude com probabilidade 0 nunca é sinalizada: recall máximo 0,75.
        var previsoes = Previsoes([0.8f, 0.8f, 0.8f, 0f], [.. Enumerable.Repeat(0.1f, 10), 0.85f]);

        var escolha = AvaliadorLimiares.Recomendar(previsoes, metaRecall: 0.9, metaTaxaSinalizada: 0.05);

        escolha.MetaAtingida.ShouldBeFalse();
        escolha.Limiar.ShouldBeInRange(0.11, 0.80);
        escolha.Recall.ShouldBe(0.75);
        escolha.Precisao.ShouldBe(0.75);
        escolha.F1.ShouldBe(0.75, tolerance: 1e-9);
    }

    [Fact]
    public void Curva_De5Em5Pontos_ComATaxaSinalizada()
    {
        var previsoes = Previsoes([0.7f], [0.3f, 0.3f, 0.3f]);

        var curva = AvaliadorLimiares.Curva(previsoes);

        curva.Count.ShouldBe(19);
        curva[0].Limiar.ShouldBe(0.05, tolerance: 1e-9);
        curva[^1].Limiar.ShouldBe(0.95, tolerance: 1e-9);
        curva.Single(ponto => Math.Abs(ponto.Limiar - 0.25) < 1e-9).TaxaSinalizada.ShouldBe(1);
        curva.Single(ponto => Math.Abs(ponto.Limiar - 0.5) < 1e-9).ShouldBe(new(0.5, Precisao: 1, Recall: 1, TaxaSinalizada: 0.25));
        curva.Single(ponto => Math.Abs(ponto.Limiar - 0.75) < 1e-9).Recall.ShouldBe(0);
    }
}
