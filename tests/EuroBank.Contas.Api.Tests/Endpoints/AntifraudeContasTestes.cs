using System.Net;
using System.Net.Http.Json;
using EuroBank.Contas.Api.Tests.Fixtures;
using EuroBank.Contas.Application.Enums;
using EuroBank.Contas.Application.Interfaces;
using EuroBank.Shared.ValueObjects;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using static EuroBank.Contas.Api.Tests.Fixtures.ClienteContas;

namespace EuroBank.Contas.Api.Tests.Endpoints;

/// <summary>
/// Saque e transferência interna perguntam ao antifraude antes de executar: só a aprovada mexe no saldo; recusa e
/// revisão respondem 422 antifraude_revisao, e sem resposta, 422 antifraude_indisponivel. A origem dos headers vai junto.
/// </summary>
public sealed class AntifraudeContasTestes : IAsyncDisposable
{
    private readonly AnaliseRiscoFalsa _antifraude = new();
    private readonly Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<MarcadorContasApi> _api;
    private readonly ClienteContas _cliente;

    public AntifraudeContasTestes(PostgresFixture banco)
    {
        _api = banco.Api.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddSingleton<IAnaliseRisco>(_antifraude)));
        var http = _api.CreateClient();
        http.DefaultRequestHeaders.Add("X-Api-Key", ContasApiFactory.ApiKey);
        _cliente = new ClienteContas(http);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task SaqueAprovado_Debita_EMandaAOrigem()
    {
        var conta = await _cliente.AbrirAsync(depositoInicial: 500m);
        using var pedido = new HttpRequestMessage(HttpMethod.Post, $"/contas/{conta.Id}/saques") { Content = JsonContent.Create(new { valor = 100m }) };
        pedido.Headers.Add("X-Dispositivo-Id", "celular-da-ana");
        pedido.Headers.Add("X-Localizacao", "48.8566,2.3522,FR");

        var depois = await LerContaAsync(await _cliente.Http.SendAsync(pedido, Ct), HttpStatusCode.OK);

        depois.Saldo.ShouldBe(400m);
        var operacao = _antifraude.Recebidas.Single(recebida => recebida.ContaId == conta.Id);
        operacao.Tipo.ShouldBe(TipoOperacaoRisco.Saque);
        operacao.Valor.Valor.ShouldBe(100m);
        operacao.Valor.Moeda.ToString().ShouldBe(conta.Moeda);
        operacao.IbanDestino.ShouldBeNull();
        operacao.DispositivoId.ShouldBe("celular-da-ana");
        operacao.Localizacao.ShouldBe(new Localizacao(48.8566, 2.3522, "FR"));
    }

    [Theory]
    [InlineData(DecisaoRisco.EmRevisao)]
    [InlineData(DecisaoRisco.Recusada)]
    public async Task SaqueNaoAprovado_422_SemDebito(DecisaoRisco decisao)
    {
        var conta = await _cliente.AbrirAsync(depositoInicial: 500m);
        _antifraude.Responder(conta.Id, decisao);

        await DeveSerProblemaAsync(await _cliente.SacarAsync(conta.Id, 100m), HttpStatusCode.UnprocessableEntity, "antifraude_revisao");

        (await _cliente.ObterAsync(conta.Id)).Saldo.ShouldBe(500m);
    }

    [Fact]
    public async Task AntifraudeForaDoAr_422_SemDebito()
    {
        var conta = await _cliente.AbrirAsync(depositoInicial: 500m);
        _antifraude.ForaDoAr(conta.Id);

        await DeveSerProblemaAsync(await _cliente.SacarAsync(conta.Id, 100m), HttpStatusCode.UnprocessableEntity, "antifraude_indisponivel");

        (await _cliente.ObterAsync(conta.Id)).Saldo.ShouldBe(500m);
    }

    [Fact]
    public async Task TransferenciaInterna_MandaODestino_NaoAprovada_SemMexerNasContas()
    {
        var origem = await _cliente.AbrirAsync(depositoInicial: 300m);
        var destino = await _cliente.AbrirAsync();
        _antifraude.Responder(origem.Id, DecisaoRisco.EmRevisao);

        await DeveSerProblemaAsync(
            await _cliente.TransferirAsync(origem.Id, destino.Iban, 100m), HttpStatusCode.UnprocessableEntity, "antifraude_revisao");

        var operacao = _antifraude.Recebidas.Single(recebida => recebida.ContaId == origem.Id);
        operacao.Tipo.ShouldBe(TipoOperacaoRisco.TransferenciaInterna);
        operacao.IbanDestino.ShouldBe(destino.Iban);
        (await _cliente.ObterAsync(origem.Id)).Saldo.ShouldBe(300m);
        (await _cliente.ObterAsync(destino.Id)).Saldo.ShouldBe(0m);
    }

    [Fact]
    public async Task LocalizacaoForaDoFormato_400_SemPerguntarAoAntifraude()
    {
        var conta = await _cliente.AbrirAsync(depositoInicial: 500m);
        using var pedido = new HttpRequestMessage(HttpMethod.Post, $"/contas/{conta.Id}/saques") { Content = JsonContent.Create(new { valor = 100m }) };
        pedido.Headers.Add("X-Localizacao", "Paris");

        await DeveSerProblemaAsync(await _cliente.Http.SendAsync(pedido, Ct), HttpStatusCode.BadRequest, "localizacao_invalida");

        _antifraude.Recebidas.ShouldNotContain(recebida => recebida.ContaId == conta.Id);
    }

    public async ValueTask DisposeAsync() => await _api.DisposeAsync();
}
