using EuroBank.Antifraude.Api.Tests.Fixtures;
using EuroBank.Antifraude.Features.ValueObjects;
using EuroBank.Antifraude.Infrastructure.MachineLearning;

namespace EuroBank.Antifraude.Api.Tests.MachineLearning;

/// <summary>O arquivo do treino carregado como a produção o usa: validado na carga e seguro com várias threads.</summary>
public sealed class ModeloCarregadoTestes
{
    private static readonly VetorFeatures Suspeita = new(8, 4, 3, 1, 5, 3, 0.95f, 8.5f, 1, 0, 0);

    [Fact]
    public void ArquivoQueNaoEModelo_InvalidDataException()
    {
        Should.Throw<InvalidDataException>(() => ModeloCarregado.Carregar(1, [1, 2, 3]));
    }

    [Fact]
    public void Contribuicoes_UmaPorFeature_NaOrdemDoVetor()
    {
        var modelo = ModeloCarregado.Carregar(7, ModelosDeTeste.Resultado.Arquivo);

        var pontuacao = modelo.Pontuar(Suspeita);

        pontuacao.Versao.ShouldBe(7);
        pontuacao.Contribuicoes.Select(contribuicao => contribuicao.Feature).ShouldBe(VetorFeatures.Nomes);
        pontuacao.Probabilidade.ShouldBeInRange(0f, 1f);
    }

    [Fact]
    public async Task VariasThreads_MesmaPontuacao()
    {
        var modelo = ModeloCarregado.Carregar(7, ModelosDeTeste.Resultado.Arquivo);
        var esperada = modelo.Pontuar(Suspeita).Probabilidade;

        var probabilidades = await Task.WhenAll(Enumerable.Range(0, 400).Select(_ => Task.Run(() => modelo.Pontuar(Suspeita).Probabilidade)));

        probabilidades.ShouldAllBe(probabilidade => probabilidade == esperada);
    }
}
