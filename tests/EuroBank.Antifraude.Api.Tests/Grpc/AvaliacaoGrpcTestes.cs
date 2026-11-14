using EuroBank.Antifraude.Api.Tests.Fixtures;
using EuroBank.Antifraude.Grpc;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using static EuroBank.Antifraude.Api.Tests.Fixtures.CenarioAvaliacao;

namespace EuroBank.Antifraude.Api.Tests.Grpc;

/// <summary>O AntifraudeService por gRPC: decisão, idempotência pela operação, consulta, simulação e erros.</summary>
public sealed class AvaliacaoGrpcTestes(AntifraudeFixture antifraude)
{
    private readonly CenarioAvaliacao _cenario = new(antifraude);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private AntifraudeService.AntifraudeServiceClient Cliente => antifraude.CriarClienteGrpc();

    [Fact]
    public async Task OperacaoComum_DeContaComHistorico_Aprovada()
    {
        var conta = await _cenario.ContaComHistoricoAsync();

        var resultado = await Cliente.AvaliarAsync(Pedido(conta), cancellationToken: Ct);

        resultado.Decisao.ShouldBe(Decisao.Aprovada);
        resultado.ScoreFinal.ShouldBe(0);
        Guid.TryParse(resultado.AnaliseId, out _).ShouldBeTrue();
        resultado.CasoId.ShouldBeEmpty();
        resultado.Sinais.Count.ShouldBe(9);
        resultado.Sinais.ShouldAllBe(sinal => !sinal.Disparou);
        resultado.DecididaEm.ShouldNotBeNull();
        resultado.VersaoConfiguracao.ShouldBeGreaterThanOrEqualTo(1);
        resultado.Simulacao.ShouldBeFalse();
    }

    [Fact]
    public async Task IbanNaListaDeBloqueio_Recusada()
    {
        var conta = await _cenario.ContaComHistoricoAsync();

        var resultado = await Cliente.AvaliarAsync(Pedido(conta, iban: IbanBloqueado), cancellationToken: Ct);

        resultado.Decisao.ShouldBe(Decisao.Recusada);
        var bloqueio = resultado.Sinais.Single(sinal => sinal.Codigo == "ListaBloqueio");
        bloqueio.Disparou.ShouldBeTrue();
        bloqueio.Tipo.ShouldBe("DURA");
    }

    [Fact]
    public async Task ScoreEntreOsLimiares_VaiParaRevisao_ComCasoAberto()
    {
        var conta = await _cenario.ContaComHistoricoAsync();

        // 4.600: mais de 5 vezes a média (25) e mais de 90% do saldo (25) = 50.
        var resultado = await Cliente.AvaliarAsync(Pedido(conta, 4_600m), cancellationToken: Ct);

        resultado.Decisao.ShouldBe(Decisao.EmRevisao);
        resultado.ScoreFinal.ShouldBe(50);
        Guid.TryParse(resultado.CasoId, out var casoId).ShouldBeTrue();
        resultado.DecididaEm.ShouldBeNull();
        await using var contexto = antifraude.CriarContexto();
        (await contexto.Casos.AsNoTracking().SingleAsync(caso => caso.OperacaoId == Guid.Parse(resultado.OperacaoId), Ct))
            .Id.Valor.ShouldBe(casoId);
    }

    [Fact]
    public async Task MesmaOperacaoDuasVezes_DevolveAMesmaAnaliseEOMesmoCaso()
    {
        var conta = await _cenario.ContaComHistoricoAsync();
        var pedido = Pedido(conta, 4_600m);

        var primeira = await Cliente.AvaliarAsync(pedido, cancellationToken: Ct);
        var segunda = await Cliente.AvaliarAsync(pedido, cancellationToken: Ct);

        segunda.AnaliseId.ShouldBe(primeira.AnaliseId);
        segunda.CasoId.ShouldBe(primeira.CasoId);
        segunda.Decisao.ShouldBe(Decisao.EmRevisao);
    }

    [Fact]
    public async Task MesmaOperacaoAoMesmoTempo_UmaAnaliseSo()
    {
        var conta = await _cenario.ContaComHistoricoAsync();
        var pedido = Pedido(conta, 4_600m);

        var resultados = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => Cliente.AvaliarAsync(pedido, cancellationToken: Ct).ResponseAsync));

        resultados.Select(resultado => resultado.AnaliseId).Distinct().ShouldHaveSingleItem();
        resultados.Select(resultado => resultado.CasoId).Distinct().ShouldHaveSingleItem();
        await using var contexto = antifraude.CriarContexto();
        var operacao = Guid.Parse(pedido.OperacaoId);
        (await contexto.Analises.CountAsync(analise => analise.OperacaoId == operacao, Ct)).ShouldBe(1);
        (await contexto.Casos.CountAsync(caso => caso.OperacaoId == operacao, Ct)).ShouldBe(1);
    }

    [Fact]
    public async Task ObterAnalise_DevolveAAvaliacaoGravada()
    {
        var conta = await _cenario.ContaComHistoricoAsync();
        var avaliada = await Cliente.AvaliarAsync(Pedido(conta, 4_600m), cancellationToken: Ct);

        var obtida = await Cliente.ObterAnaliseAsync(new ObterAnaliseRequest { AnaliseId = avaliada.AnaliseId }, cancellationToken: Ct);

        obtida.AnaliseId.ShouldBe(avaliada.AnaliseId);
        obtida.CasoId.ShouldBe(avaliada.CasoId);
        obtida.ScoreFinal.ShouldBe(avaliada.ScoreFinal);
        obtida.Sinais.Count.ShouldBe(avaliada.Sinais.Count);
    }

    [Fact]
    public async Task ObterAnalise_Desconhecida_NotFound()
    {
        var erro = await Should.ThrowAsync<RpcException>(() =>
            Cliente.ObterAnaliseAsync(new ObterAnaliseRequest { AnaliseId = Guid.CreateVersion7().ToString() }, cancellationToken: Ct).ResponseAsync);

        erro.StatusCode.ShouldBe(StatusCode.NotFound);
    }

    [Fact]
    public async Task Simular_AvaliaSemGravar()
    {
        var conta = await _cenario.ContaComHistoricoAsync();
        var pedido = Pedido(conta, 4_600m);

        var simulada = await Cliente.SimularAsync(pedido, cancellationToken: Ct);

        simulada.Simulacao.ShouldBeTrue();
        simulada.Decisao.ShouldBe(Decisao.EmRevisao);
        simulada.AnaliseId.ShouldBeEmpty();
        simulada.CasoId.ShouldBeEmpty();
        await using var contexto = antifraude.CriarContexto();
        var operacao = Guid.Parse(pedido.OperacaoId);
        (await contexto.Analises.AnyAsync(analise => analise.OperacaoId == operacao, Ct)).ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("chave-errada")]
    public async Task SemApiKeyOuComKeyErrada_Unauthenticated(string? apiKey)
    {
        var cliente = antifraude.CriarClienteGrpc(apiKey);

        var erro = await Should.ThrowAsync<RpcException>(() =>
            cliente.AvaliarAsync(Pedido(Guid.CreateVersion7()), cancellationToken: Ct).ResponseAsync);

        erro.StatusCode.ShouldBe(StatusCode.Unauthenticated);
    }

    public static TheoryData<string, Action<SolicitacaoAvaliacao>> PedidosInvalidos => new()
    {
        { "operacao_id inválido", pedido => pedido.OperacaoId = "não é guid" },
        { "conta_id inválido", pedido => pedido.ContaId = string.Empty },
        { "tipo não informado", pedido => pedido.TipoOperacao = TipoOperacao.NaoInformado },
        { "valor zero", pedido => pedido.Valor = 0m },
        { "valor com três casas", pedido => pedido.Valor = 10.123m },
        { "valor ausente", pedido => pedido.Valor = null },
        { "moeda desconhecida", pedido => pedido.Moeda = "XYZ" },
    };

    [Theory]
    [MemberData(nameof(PedidosInvalidos))]
    public async Task PedidoInvalido_InvalidArgument(string caso, Action<SolicitacaoAvaliacao> estragar)
    {
        var pedido = Pedido(Guid.CreateVersion7());
        estragar(pedido);

        var erro = await Should.ThrowAsync<RpcException>(() => Cliente.AvaliarAsync(pedido, cancellationToken: Ct).ResponseAsync);

        erro.StatusCode.ShouldBe(StatusCode.InvalidArgument, caso);
    }

    [Fact]
    public async Task RedisForaDoAr_Unavailable_FailClosed()
    {
        await using var semRedis = antifraude.CriarApi("127.0.0.1:1,connectTimeout=300,syncTimeout=300");
        var cliente = antifraude.CriarClienteGrpc(api: semRedis);

        var erro = await Should.ThrowAsync<RpcException>(() =>
            cliente.AvaliarAsync(Pedido(Guid.CreateVersion7()), cancellationToken: Ct).ResponseAsync);

        erro.StatusCode.ShouldBe(StatusCode.Unavailable);
    }
}
