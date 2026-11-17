using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.ValueObjects;
using EuroBank.Antifraude.Features.ValueObjects;
using EuroBank.Antifraude.Treinamento.Data;
using EuroBank.Antifraude.Treinamento.Tests.Fixtures;
using EuroBank.Antifraude.Treinamento.Training;
using EuroBank.Shared.Enums;
using EuroBank.Shared.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace EuroBank.Antifraude.Treinamento.Tests.Data;

/// <summary>
/// O treino e o banco do Antifraude: quais análises viram rótulo (e qual) e a versão registrada voltando inteira. Só
/// o teste de rótulos grava análises, então ele conta todas as que existem.
/// </summary>
public sealed class BancoTreinoTestes(BancoAntifraudeFixture banco) : IClassFixture<BancoAntifraudeFixture>
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 15, 10, 0, 0, TimeSpan.Zero);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Uma análise com features distintas (o valor sobre a média identifica cada uma).</summary>
    private static Analise NovaAnalise(DecisaoAnalise decisao, float marca, bool comFeatures = true)
    {
        var operacao = new ContextoOperacao(
            Guid.CreateVersion7(), TipoOperacao.Externa, Guid.CreateVersion7(), Dinheiro.De(100m, Moeda.EUR), "DE89370400440532013000", Agora);
        var features = comFeatures ? new VetorFeatures(marca, 0, 0, 1, 400, 14, 0.1f, 4.6f, 0, 0, 0) : null;
        return Analise.Registrar(operacao, new ResultadoMotor(decisao, 50, [], 1, features), Agora);
    }

    [Fact]
    public async Task RotulosReais_SoAsDecididasPorAnalistaEAsConfirmadas()
    {
        var recusada = NovaAnalise(DecisaoAnalise.EmRevisao, 1);
        recusada.AplicarRevisao(aprovar: false, "ana", "golpe do falso parente", Agora);
        var aprovada = NovaAnalise(DecisaoAnalise.EmRevisao, 2);
        aprovada.AplicarRevisao(aprovar: true, "ana", "cliente confirmou", Agora);
        var expirada = NovaAnalise(DecisaoAnalise.EmRevisao, 3);
        expirada.Expirar(Agora);
        var cancelada = NovaAnalise(DecisaoAnalise.EmRevisao, 4);
        cancelada.Cancelar(Agora);
        var peloMotor = NovaAnalise(DecisaoAnalise.Aprovada, 5);
        var confirmadaDepois = NovaAnalise(DecisaoAnalise.Aprovada, 6);
        var aprovadaEConfirmada = NovaAnalise(DecisaoAnalise.EmRevisao, 7);
        aprovadaEConfirmada.AplicarRevisao(aprovar: true, "ana", "parecia normal", Agora);
        var semFeatures = NovaAnalise(DecisaoAnalise.EmRevisao, 8, comFeatures: false);
        semFeatures.AplicarRevisao(aprovar: false, "ana", "antes do modelo", Agora);

        await using (var contexto = banco.CriarContexto())
        {
            contexto.Analises.AddRange(recusada, aprovada, expirada, cancelada, peloMotor, confirmadaDepois, aprovadaEConfirmada, semFeatures);
            contexto.FraudesConfirmadas.AddRange(
                FraudeConfirmada.Registrar(confirmadaDepois, "beto", "reclamação do cliente", Agora),
                FraudeConfirmada.Registrar(aprovadaEConfirmada, "beto", null, Agora));
            await contexto.SaveChangesAsync(Ct);
        }

        IReadOnlyList<LinhaTreino> rotulos;
        await using (var contexto = banco.CriarContexto())
        {
            rotulos = await RotulosReais.CarregarAsync(contexto, Ct);
        }

        rotulos.ShouldAllBe(linha => linha.Real);
        rotulos.ToDictionary(linha => linha.Features.ValorSobreMedia, linha => linha.Fraude)
            .ShouldBe(new Dictionary<float, bool> { [1] = true, [2] = false, [6] = true, [7] = true }, ignoreOrder: true);
    }

    [Fact]
    public async Task Versao_RegistradaVoltaInteira()
    {
        var resultado = new ResultadoTreino(
            Arquivo: [1, 2, 3],
            new MetricasModelo(0.81, 0.4, 0.92, 0.56, new MatrizConfusao(46, 69, 9_800, 4), [new PontoCurva(0.5, 0.4, 0.92, 0.012)]),
            LimiarRecomendado: 0.37,
            new DadosTreino(42, 99_167, 1_464, 3, 1, Agora.AddDays(-90), Agora.AddDays(-20), Agora, 21_000, 0.9, 0.05, MetaAtingida: true),
            [new DistribuicaoFeature("LogValor", [1.5, 3.2], [0.3, 0.5, 0.2]), new DistribuicaoFeature(DistribuicaoFeature.NomeScore, [0.1], [0.9, 0.1])]);

        VersaoModelo registrada;
        await using (var contexto = banco.CriarContexto())
        {
            registrada = await RegistroModelos.RegistrarAsync(contexto, resultado, Agora, Ct);
        }

        registrada.Versao.ShouldBeGreaterThan(0);
        await using var leitura = banco.CriarContexto();
        var lida = await leitura.VersoesModelo.SingleAsync(versao => versao.Versao == registrada.Versao, Ct);
        lida.Status.ShouldBe(StatusModelo.Registrada);
        lida.Arquivo.ShouldBe(resultado.Arquivo);
        lida.LimiarRecomendado.ShouldBe(0.37);
        lida.TreinadaEm.ShouldBe(Agora);
        lida.Dados.ShouldBe(resultado.Dados);
        lida.Metricas.Curva.ShouldBe(resultado.Metricas.Curva);
        (lida.Metricas with { Curva = resultado.Metricas.Curva }).ShouldBe(resultado.Metricas);
        lida.Distribuicao.Select(feature => feature.Nome).ShouldBe(resultado.Distribuicao.Select(feature => feature.Nome));
        lida.Distribuicao.SelectMany(feature => feature.Limites).ShouldBe(resultado.Distribuicao.SelectMany(feature => feature.Limites));
        lida.Distribuicao.SelectMany(feature => feature.Proporcoes).ShouldBe(resultado.Distribuicao.SelectMany(feature => feature.Proporcoes));
    }
}
