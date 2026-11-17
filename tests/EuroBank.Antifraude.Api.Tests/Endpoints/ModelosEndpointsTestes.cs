using System.Net;
using EuroBank.Antifraude.Api.Tests.Fixtures;
using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.ValueObjects;
using EuroBank.Antifraude.Features.ValueObjects;
using EuroBank.Antifraude.Grpc;
using EuroBank.Antifraude.Infrastructure.MachineLearning;
using Microsoft.Extensions.DependencyInjection;
using static EuroBank.Antifraude.Api.Tests.Fixtures.CenarioAvaliacao;
using static EuroBank.Antifraude.Api.Tests.Fixtures.ClienteAnalista;

namespace EuroBank.Antifraude.Api.Tests.Endpoints;

/// <summary>
/// O modelo em produção: promover troca o modelo sem reiniciar, o challenger pontua em sombra, a análise traz o score
/// e a explicação, e uma versão que não carrega não derruba a anterior. Valem para todas as avaliações: rodam na
/// coleção global e cada teste começa e termina sem modelo em uso.
/// </summary>
[Collection(nameof(ColecaoConfiguracaoGlobal))]
public sealed class ModelosEndpointsTestes(AntifraudeFixture antifraude) : IAsyncLifetime, IDisposable
{
    private readonly ClienteAnalista _analista = new(antifraude);

    private readonly CenarioAvaliacao _cenario = new(antifraude);

    private readonly ModelosDeTeste _modelos = new(antifraude);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await _modelos.LimparAsync();

    public async ValueTask DisposeAsync() => await _modelos.LimparAsync();

    /// <summary>Uma externa comum de 100 numa conta com histórico: nenhuma regra dispara, só o modelo pontua.</summary>
    private async Task<ResultadoAvaliacao> AvaliarComumAsync()
    {
        var conta = await _cenario.ContaComHistoricoAsync();
        return await antifraude.CriarClienteGrpc().AvaliarAsync(Pedido(conta), cancellationToken: Ct);
    }

    private async Task AtivarAsync(int versao) =>
        (await OkAsync(await _analista.PostAsync($"/modelos/{versao}/ativacao", new { })))
            .GetProperty("status").GetString().ShouldBe("Ativa");

    private async Task ColocarEmSombraAsync(int versao) =>
        (await OkAsync(await _analista.PostAsync($"/modelos/{versao}/challenger", new { })))
            .GetProperty("status").GetString().ShouldBe("Challenger");

    private async Task<string?> StatusAsync(int versao) => (await _analista.GetAsync($"/modelos/{versao}")).GetProperty("status").GetString();

    [Fact]
    public async Task SemModeloAtivo_SoAsRegras()
    {
        var resultado = await AvaliarComumAsync();

        resultado.Modelo.ShouldBeNull();
        resultado.Challenger.ShouldBeNull();
        resultado.Sinais.ShouldNotContain(sinal => sinal.Codigo == "ModeloFraude");
    }

    [Fact]
    public async Task Ativar_TrocaSemReiniciar_EAAnaliseTrazScoreEExplicacao()
    {
        var versao = await _modelos.RegistrarAsync();
        (await StatusAsync(versao)).ShouldBe("Registrada");

        await AtivarAsync(versao);
        await _modelos.EsperarAsync(emUso => emUso.Ativo?.Versao == versao, $"a versão {versao} ativa");
        var resultado = await AvaliarComumAsync();

        var modelo = resultado.Modelo.ShouldNotBeNull();
        modelo.Versao.ShouldBe(versao);
        modelo.Probabilidade.ShouldBeInRange(0, 1);
        // A probabilidade sai com quatro casas: os pontos ficam a meio ponto (e um pouco) de probabilidade x 50.
        ((double)modelo.Pontos).ShouldBe(modelo.Probabilidade * 50, tolerance: 0.51);
        modelo.Contribuicoes.Count.ShouldBe(11);
        modelo.Contribuicoes.Select(contribuicao => Math.Abs(contribuicao.Valor)).ShouldBeInOrder(SortDirection.Descending);
        var sinal = resultado.Sinais.Single(sinal => sinal.Codigo == "ModeloFraude");
        sinal.Tipo.ShouldBe("PESO");
        sinal.Pontos.ShouldBe(modelo.Pontos);
        resultado.ScoreFinal.ShouldBe(modelo.Pontos);

        // Gravado com a análise: a consulta devolve o mesmo.
        var lida = await antifraude.CriarClienteGrpc().ObterAnaliseAsync(new ObterAnaliseRequest { AnaliseId = resultado.AnaliseId }, cancellationToken: Ct);
        lida.Modelo.ShouldBe(modelo);
    }

    [Fact]
    public async Task Challenger_PontuaEmSombra_EPromovidoViraOAtivoSemReiniciar()
    {
        var ativa = await _modelos.RegistrarAsync();
        var challenger = await _modelos.RegistrarAsync();
        await AtivarAsync(ativa);
        await ColocarEmSombraAsync(challenger);
        await _modelos.EsperarAsync(
            emUso => emUso.Ativo?.Versao == ativa && emUso.Challenger?.Versao == challenger, "ativa e challenger carregados");

        var comSombra = await AvaliarComumAsync();
        comSombra.Modelo.ShouldNotBeNull().Versao.ShouldBe(ativa);
        comSombra.Challenger.ShouldNotBeNull().Versao.ShouldBe(challenger);
        comSombra.Sinais.Count(sinal => sinal.Codigo == "ModeloFraude").ShouldBe(1);
        comSombra.ScoreFinal.ShouldBe(comSombra.Modelo.Pontos);

        await AtivarAsync(challenger);
        await _modelos.EsperarAsync(emUso => emUso.Ativo?.Versao == challenger && emUso.Challenger is null, "o challenger promovido");
        (await StatusAsync(ativa)).ShouldBe("Aposentada");

        var promovido = await AvaliarComumAsync();
        promovido.Modelo.ShouldNotBeNull().Versao.ShouldBe(challenger);
        promovido.Challenger.ShouldBeNull();
    }

    [Fact]
    public async Task RetirarChallenger_204_ASombraFicaVazia()
    {
        var versao = await _modelos.RegistrarAsync();
        await ColocarEmSombraAsync(versao);
        await _modelos.EsperarAsync(emUso => emUso.Challenger?.Versao == versao, "o challenger carregado");

        (await _analista.DeleteAsync("/modelos/challenger")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await _modelos.EsperarAsync(emUso => emUso.Challenger is null, "a sombra vazia");
        (await StatusAsync(versao)).ShouldBe("Aposentada");
        (await AvaliarComumAsync()).Challenger.ShouldBeNull();

        (await _analista.DeleteAsync("/modelos/challenger")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task TransicaoInvalida_409_VersaoInexistente_404()
    {
        var versao = await _modelos.RegistrarAsync();
        await AtivarAsync(versao);

        await DeveSerProblemaAsync(
            await _analista.PostAsync($"/modelos/{versao}/challenger", new { }), HttpStatusCode.Conflict, "transicao_modelo_invalida");
        await DeveSerProblemaAsync(await _analista.PostAsync("/modelos/999999/ativacao", new { }), HttpStatusCode.NotFound, "modelo_nao_encontrado");
        await DeveSerProblemaAsync(await _analista.Http.GetAsync("/modelos/999999", Ct), HttpStatusCode.NotFound, "modelo_nao_encontrado");

        // Ativar a que já está ativa não muda nada.
        await AtivarAsync(versao);
    }

    [Fact]
    public async Task VersaoQueNaoCarrega_FicaAAnterior_EAAvaliacaoSegue()
    {
        var boa = await _modelos.RegistrarAsync();
        await AtivarAsync(boa);
        await _modelos.EsperarAsync(emUso => emUso.Ativo?.Versao == boa, "a versão boa ativa");

        var ruim = await _modelos.RegistrarAsync([1, 2, 3]);
        await AtivarAsync(ruim);
        // Recarga explícita (além da que o aviso dispara), para o teste não depender de tempo.
        await antifraude.Api.Services.GetRequiredService<CarregadorModelos>().RecarregarAsync(Ct);

        _modelos.EmUso.Ativo.ShouldNotBeNull().Versao.ShouldBe(boa);
        (await AvaliarComumAsync()).Modelo.ShouldNotBeNull().Versao.ShouldBe(boa);
    }

    [Fact]
    public async Task Detalhe_MetricasCurvaEDistribuicao_SemOArquivo()
    {
        var versao = await _modelos.RegistrarAsync();

        var lista = await _analista.GetAsync("/modelos");
        var resumo = lista.EnumerateArray().Single(item => item.GetProperty("versao").GetInt32() == versao);
        resumo.GetProperty("aucPr").GetDouble().ShouldBeGreaterThan(0.6);
        lista[0].GetProperty("versao").GetInt32().ShouldBe(versao);

        var detalhe = await _analista.GetAsync($"/modelos/{versao}");
        detalhe.GetProperty("metricas").GetProperty("curva").GetArrayLength().ShouldBe(19);
        detalhe.GetProperty("metricas").GetProperty("matriz").GetProperty("verdadeirosPositivos").GetInt32().ShouldBeGreaterThan(0);
        detalhe.GetProperty("dados").GetProperty("semente").GetInt32().ShouldBe(ModelosDeTeste.SementeDados);
        detalhe.GetProperty("distribuicao").GetArrayLength().ShouldBe(12);
        detalhe.GetProperty("tamanhoArquivo").GetInt32().ShouldBe(ModelosDeTeste.Resultado.Arquivo.Length);
        detalhe.TryGetProperty("arquivo", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task CasoEmRevisao_MostraFeaturesScoreEExplicacao_PesoZeroNaoPontua()
    {
        var versao = await _modelos.RegistrarAsync();
        await AtivarAsync(versao);
        await _modelos.EsperarAsync(emUso => emUso.Ativo?.Versao == versao, $"a versão {versao} ativa");
        await OkAsync(await _analista.PutAsync("/configuracao-decisao", new { limiarRevisao = 30, limiarRecusa = 70, pesoModelo = 0 }));
        try
        {
            var (casoId, _) = await _analista.CasoPendenteAsync();

            var analise = (await _analista.GetAsync($"/casos/{casoId}")).GetProperty("analise");
            analise.GetProperty("scoreFinal").GetInt32().ShouldBe(50);
            analise.GetProperty("features").EnumerateObject().Count().ShouldBe(11);
            var modelo = analise.GetProperty("modelo");
            modelo.GetProperty("versao").GetInt32().ShouldBe(versao);
            modelo.GetProperty("pontos").GetInt32().ShouldBe(0);
            modelo.GetProperty("contribuicoes").GetArrayLength().ShouldBe(11);
            analise.GetProperty("challenger").ValueKind.ShouldBe(System.Text.Json.JsonValueKind.Null);
            analise.GetProperty("sinais").EnumerateArray()
                .Single(sinal => sinal.GetProperty("codigo").GetString() == "ModeloFraude")
                .GetProperty("disparou").GetBoolean().ShouldBeFalse();
        }
        finally
        {
            await OkAsync(await _analista.PutAsync("/configuracao-decisao", new { limiarRevisao = 30, limiarRecusa = 70, pesoModelo = 50 }));
        }
    }

    [Fact]
    public async Task VisaoGeral_DriftDaVersaoAtiva_DaUltimaMedicao()
    {
        (await _analista.GetAsync("/visao-geral")).GetProperty("drift").GetProperty("situacao").GetString().ShouldBe("SemModeloAtivo");
        var versao = await _modelos.RegistrarAsync();
        await AtivarAsync(versao);

        var aguardando = (await _analista.GetAsync("/visao-geral")).GetProperty("drift");
        aguardando.GetProperty("situacao").GetString().ShouldBe("AguardandoMedicao");
        aguardando.GetProperty("versaoModelo").GetInt32().ShouldBe(versao);

        // Uma medição como o worker gravaria: 150 análises iguais, bem diferentes do treino.
        var agora = DateTimeOffset.UtcNow;
        var features = Enumerable.Repeat(new VetorFeatures(9, 5, 4, 1, 2, 3, 0.99f, 9, 1, 0, 3), 150).ToList();
        await using (var contexto = antifraude.CriarContexto())
        {
            contexto.MedicoesDrift.Add(MedicaoDrift.Medir(
                versao, ModelosDeTeste.Resultado.Distribuicao, features, [], LimitesDrift.Padrao, agora.AddHours(-24), agora));
            await contexto.SaveChangesAsync(Ct);
        }

        var drift = (await _analista.GetAsync("/visao-geral")).GetProperty("drift");
        drift.GetProperty("situacao").GetString().ShouldBe("Medido");
        var medicao = drift.GetProperty("ultimaMedicao");
        medicao.GetProperty("versaoModelo").GetInt32().ShouldBe(versao);
        medicao.GetProperty("amostra").GetInt32().ShouldBe(150);
        medicao.GetProperty("nivel").GetString().ShouldBe("Significativo");
        var psis = medicao.GetProperty("psis").EnumerateArray().Select(psi => psi.GetProperty("psi").GetDouble()).ToList();
        psis.Count.ShouldBe(11, "sem análises pontuadas, o score fica de fora");
        psis.ShouldBeInOrder(SortDirection.Descending);
    }

    public void Dispose() => _analista.Dispose();
}
