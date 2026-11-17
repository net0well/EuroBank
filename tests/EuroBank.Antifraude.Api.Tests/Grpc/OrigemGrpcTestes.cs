using EuroBank.Antifraude.Api.Tests.Fixtures;
using EuroBank.Antifraude.Application.TransferObjects;
using EuroBank.Antifraude.Grpc;
using Grpc.Core;
using static EuroBank.Antifraude.Api.Tests.Fixtures.CenarioAvaliacao;
using LocalizacaoCliente = EuroBank.Shared.ValueObjects.Localizacao;

namespace EuroBank.Antifraude.Api.Tests.Grpc;

/// <summary>
/// Dispositivo e localização no Avaliar: guardados na análise e conferidos contra as operações aprovadas da conta
/// (dispositivo novo, viagem impossível); fora dos limites, InvalidArgument.
/// </summary>
public sealed class OrigemGrpcTestes(AntifraudeFixture antifraude)
{
    private readonly CenarioAvaliacao _cenario = new(antifraude);

    private readonly ClienteAnalista _analista = new(antifraude);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private AntifraudeService.AntifraudeServiceClient Cliente => antifraude.CriarClienteGrpc();

    private static SolicitacaoAvaliacao ComOrigem(Guid conta, string dispositivo, double latitude, double longitude, string pais)
    {
        var pedido = Pedido(conta);
        pedido.DispositivoId = dispositivo;
        pedido.Localizacao = new Localizacao { Latitude = latitude, Longitude = longitude, Pais = pais };
        return pedido;
    }

    [Fact]
    public async Task ParisEUmaHoraDepoisSaoPaulo_ViagemImpossivel_Revisao_ComAOrigemNoCaso()
    {
        var conta = await _cenario.ContaComHistoricoAsync();
        await antifraude.Perfis.RegistrarUsoAsync(
            new UsoConta(conta, Guid.CreateVersion7(), Agora.AddHours(-1), "celular-da-ana", new LocalizacaoCliente(48.8566, 2.3522, "FR")), Ct);

        var resultado = await Cliente.AvaliarAsync(ComOrigem(conta, "notebook-novo", -23.5505, -46.6333, "BR"), cancellationToken: Ct);

        resultado.Decisao.ShouldBe(Decisao.EmRevisao);
        resultado.Sinais.Single(sinal => sinal.Codigo == "ViagemImpossivel").Disparou.ShouldBeTrue();
        resultado.Sinais.Single(sinal => sinal.Codigo == "DispositivoNovo").Pontos.ShouldBe(15);
        var analise = (await _analista.GetAsync($"/casos/{resultado.CasoId}")).GetProperty("analise");
        analise.GetProperty("dispositivoId").GetString().ShouldBe("notebook-novo");
        analise.GetProperty("localizacao").GetString().ShouldBe("-23.5505,-46.6333,BR");
    }

    [Fact]
    public async Task MesmoDispositivoPertoDaUltima_Aprovada()
    {
        var conta = await _cenario.ContaComHistoricoAsync();
        await antifraude.Perfis.RegistrarUsoAsync(
            new UsoConta(conta, Guid.CreateVersion7(), Agora.AddHours(-1), "celular-da-ana", new LocalizacaoCliente(48.8566, 2.3522, "FR")), Ct);

        var resultado = await Cliente.AvaliarAsync(ComOrigem(conta, "celular-da-ana", 48.86, 2.35, "FR"), cancellationToken: Ct);

        resultado.Decisao.ShouldBe(Decisao.Aprovada);
        resultado.Sinais.Where(sinal => sinal.Disparou).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(95, 0, "FR")]
    [InlineData(0, 200, "FR")]
    [InlineData(0, 0, "FRA")]
    public async Task LocalizacaoForaDosLimites_InvalidArgument(double latitude, double longitude, string pais)
    {
        var conta = Guid.CreateVersion7();

        var erro = await Should.ThrowAsync<RpcException>(() =>
            Cliente.AvaliarAsync(ComOrigem(conta, "celular", latitude, longitude, pais), cancellationToken: Ct).ResponseAsync);

        erro.StatusCode.ShouldBe(StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task DispositivoLongoDemais_InvalidArgument()
    {
        var erro = await Should.ThrowAsync<RpcException>(() =>
            Cliente.AvaliarAsync(ComOrigem(Guid.CreateVersion7(), new string('x', 101), 0, 0, "FR"), cancellationToken: Ct).ResponseAsync);

        erro.StatusCode.ShouldBe(StatusCode.InvalidArgument);
        erro.Status.Detail.ShouldContain("dispositivo_id");
    }
}
