using EuroBank.Antifraude.Domain.ValueObjects;
using EuroBank.Antifraude.Features.ValueObjects;
using EuroBank.Antifraude.Infrastructure.MachineLearning;
using EuroBank.Antifraude.Treinamento.Generation;
using EuroBank.Antifraude.Treinamento.Training;
using Microsoft.ML;

namespace EuroBank.Antifraude.Treinamento.Tests.Training;

/// <summary>
/// O treino num mundo pequeno: separa bem no teste temporal, o arquivo recarregado pontua e explica, e a mesma
/// semente dá o mesmo modelo.
/// </summary>
public sealed class TreinadorModeloTestes
{
    private static readonly OpcoesGeracao Mundo = new(Semente: 7, Contas: 400, Dias: 60);

    private static readonly Lazy<IReadOnlyList<LinhaTreino>> Sinteticas = new(() => new GeradorTransferencias(Mundo).Gerar()
        .Select(transferencia => new LinhaTreino(transferencia.Momento, transferencia.Features, transferencia.Fraude, Real: false))
        .ToList());

    /// <summary>Dois rótulos reais: uma recusa do analista e uma aprovação.</summary>
    private static readonly IReadOnlyList<LinhaTreino> Reais =
    [
        new(Mundo.Fim, Sinteticas.Value.First(linha => linha.Fraude).Features, Fraude: true, Real: true),
        new(Mundo.Fim, Sinteticas.Value.First(linha => !linha.Fraude).Features, Fraude: false, Real: true),
    ];

    private static readonly Lazy<ResultadoTreino> Resultado = new(() => new TreinadorModelo(new OpcoesTreino()).Treinar(Mundo.Semente, Sinteticas.Value, Reais));

    [Fact]
    public void VetorDoModelo_TemOTamanhoDasFeatures()
    {
        EntradaModelo.TamanhoVetor.ShouldBe(VetorFeatures.Nomes.Count);
    }

    [Fact]
    public void TesteTemporal_AucPrAcimaDoMinimo_ComOsDiasMaisRecentes()
    {
        var resultado = Resultado.Value;

        resultado.Metricas.AucPr.ShouldBeGreaterThan(0.6);
        resultado.LimiarRecomendado.ShouldBeInRange(0.01, 0.99);
        resultado.Metricas.Curva.Count.ShouldBe(19);

        var dados = resultado.Dados;
        dados.Semente.ShouldBe(Mundo.Semente);
        dados.LinhasSinteticas.ShouldBe(Sinteticas.Value.Count);
        dados.FraudesSinteticas.ShouldBe(Sinteticas.Value.Count(linha => linha.Fraude));
        dados.RotulosReais.ShouldBe(2);
        dados.FraudesReais.ShouldBe(1);
        dados.LinhasTeste.ShouldBe(Sinteticas.Value.Count(linha => linha.Momento >= dados.CorteTeste));
        ((double)dados.LinhasTeste / Sinteticas.Value.Count).ShouldBe(0.22, tolerance: 0.01);
        dados.CorteTeste.ShouldBeGreaterThan(dados.InicioTreino);
        dados.FimTeste.ShouldBeGreaterThan(dados.CorteTeste);

        var matriz = resultado.Metricas.Matriz;
        (matriz.VerdadeirosPositivos + matriz.FalsosPositivos + matriz.VerdadeirosNegativos + matriz.FalsosNegativos).ShouldBe(dados.LinhasTeste);
    }

    [Fact]
    public void ArquivoRecarregado_PontuaFraudeAcimaDoNormal_EExplicaCadaFeature()
    {
        var ml = new MLContext();
        var modelo = ml.Model.Load(new MemoryStream(Resultado.Value.Arquivo), out _);
        var motor = ml.Model.CreatePredictionEngine<EntradaModelo, SaidaModelo>(modelo);
        var teste = Sinteticas.Value.Where(linha => linha.Momento >= Resultado.Value.Dados.CorteTeste).ToList();

        var saidas = teste.Select(linha => (linha.Fraude, Saida: motor.Predict(EntradaModelo.De(linha.Features)))).ToList();

        var mediaFraude = saidas.Where(saida => saida.Fraude).Average(saida => saida.Saida.Probability);
        var mediaNormal = saidas.Where(saida => !saida.Fraude).Average(saida => saida.Saida.Probability);
        mediaFraude.ShouldBeGreaterThan(mediaNormal + 0.5f);
        saidas.ShouldAllBe(saida => saida.Saida.Probability >= 0 && saida.Saida.Probability <= 1);

        var fraude = saidas.Where(saida => saida.Fraude).MaxBy(saida => saida.Saida.Probability).Saida;
        fraude.FeatureContributions.Length.ShouldBe(VetorFeatures.Nomes.Count);
        fraude.FeatureContributions.Sum().ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Distribuicao_CadaFeatureEOScore_ProporcoesSomamUm()
    {
        var distribuicao = Resultado.Value.Distribuicao;

        distribuicao.Select(feature => feature.Nome).ShouldBe([.. VetorFeatures.Nomes, DistribuicaoFeature.NomeScore]);
        distribuicao.ShouldAllBe(feature => feature.Proporcoes.Count == feature.Limites.Count + 1);
        distribuicao.ShouldAllBe(feature => Math.Abs(feature.Proporcoes.Sum() - 1) < 1e-9);
    }

    [Fact]
    public void OutraSemente_MesmoModelo_PorqueOFastTreeNaoSorteiaNada()
    {
        // Todas as linhas e todas as features em cada árvore: sem amostragem, a semente não muda o modelo.
        var outraSemente = new TreinadorModelo(new OpcoesTreino(Semente: 99)).Treinar(Mundo.Semente, Sinteticas.Value, Reais);

        outraSemente.Metricas.AucPr.ShouldBe(Resultado.Value.Metricas.AucPr);
        outraSemente.Metricas.Curva.ShouldBe(Resultado.Value.Metricas.Curva);
    }

    [Fact]
    public void MesmaSemente_MesmoModelo()
    {
        var outraVez = new TreinadorModelo(new OpcoesTreino()).Treinar(Mundo.Semente, Sinteticas.Value, Reais);

        outraVez.Metricas.AucPr.ShouldBe(Resultado.Value.Metricas.AucPr);
        outraVez.Metricas.Matriz.ShouldBe(Resultado.Value.Metricas.Matriz);
        outraVez.Metricas.Curva.ShouldBe(Resultado.Value.Metricas.Curva);
        outraVez.LimiarRecomendado.ShouldBe(Resultado.Value.LimiarRecomendado);
    }
}
