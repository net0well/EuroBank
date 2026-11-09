using System.Text.Json;
using EuroBank.Simuladores.Cambio;
using EuroBank.Simuladores.Cambio.Persistencia;
using EuroBank.Simuladores.Cambio.Regras;
using EuroBank.Simuladores.Comum;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Testcontainers.PostgreSql;

namespace EuroBank.Simuladores.Tests.Cambio;

public sealed class CambioFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18.6")
        .WithDatabase("eurobank_cambio")
        .WithUsername("eurobank")
        .WithPassword("eurobank")
        .Build();

    public RepositorioCambio Repositorio { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        Repositorio = new RepositorioCambio(_container.GetConnectionString());
        await Repositorio.CriarEstruturaAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}

public sealed class ProcessadorCambioTestes(CambioFixture banco) : IClassFixture<CambioFixture>
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 9, 12, 0, 0, TimeSpan.Zero);

    private static readonly IOptions<OpcoesCambio> Opcoes =
        Options.Create(new OpcoesCambio { LatenciaMs = 500, ValidadeMinutos = 10, Spread = 0.01m });

    private readonly ProcessadorComandosCambio _processador = new(
        banco.Repositorio, new TabelaCambio(Opcoes), Opcoes, new FakeTimeProvider(Agora), NullLogger<ProcessadorComandosCambio>.Instance);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private Task CotarAsync(Guid transferenciaId, string origem, string destino, string? idMensagem = null) =>
        _processador.ProcessarAsync(
            new MensagemRecebida(
                idMensagem ?? Guid.CreateVersion7().ToString(),
                "CotarCambio",
                transferenciaId.ToString(),
                JsonSerializer.Serialize(new
                {
                    transferenciaId,
                    moedaOrigem = origem,
                    moedaDestino = destino,
                    valor = 100m,
                    moedaDoValor = destino,
                }, JsonSerializerOptions.Web),
                "00-4ec0e9a4e59373e59c1b456265157e28-f6186d492757de83-01"),
            Ct);

    [Fact]
    public async Task ParDisponivel_GuardaACotacaoEAgendaARespostaComALatencia()
    {
        var id = Guid.CreateVersion7();

        await CotarAsync(id, "EUR", "USD");

        var resposta = (await banco.Repositorio.ListarRespostasAsync(id, Ct)).ShouldHaveSingleItem();
        resposta.Tipo.ShouldBe("CotacaoEmitida");
        resposta.ResponderEm.ShouldBe(Agora.AddMilliseconds(500));
        resposta.Traceparent.ShouldNotBeNull();
        using var payload = JsonDocument.Parse(resposta.Payload);
        payload.RootElement.GetProperty("taxa").GetDecimal().ShouldBe(1.0692m);
        payload.RootElement.GetProperty("valorOrigem").GetDecimal().ShouldBe(93.53m);
        payload.RootElement.GetProperty("valorDestino").GetDecimal().ShouldBe(100m);
        payload.RootElement.GetProperty("validaAte").GetDateTimeOffset().ShouldBe(Agora.AddMinutes(10));

        var cotacao = (await banco.Repositorio.ListarCotacoesAsync(id, Ct)).ShouldHaveSingleItem();
        cotacao.ShouldBe(payload.RootElement.GetProperty("cotacaoId").GetGuid());
    }

    [Fact]
    public async Task ParIndisponivel_RespondeCotacaoIndisponivelSemGuardarCotacao()
    {
        var id = Guid.CreateVersion7();

        await CotarAsync(id, "BRL", "GBP");

        (await banco.Repositorio.ListarRespostasAsync(id, Ct)).ShouldHaveSingleItem().Tipo.ShouldBe("CotacaoIndisponivel");
        (await banco.Repositorio.ListarCotacoesAsync(id, Ct)).ShouldBeEmpty();
    }

    [Fact]
    public async Task CadaPedidoNovo_EmiteOutraCotacao_MasAMesmaMensagemSoUmaVez()
    {
        var id = Guid.CreateVersion7();
        var idMensagem = Guid.CreateVersion7().ToString();

        await CotarAsync(id, "EUR", "USD", idMensagem);
        await CotarAsync(id, "EUR", "USD", idMensagem);
        await CotarAsync(id, "EUR", "USD");

        (await banco.Repositorio.ListarCotacoesAsync(id, Ct)).Count.ShouldBe(2);
        (await banco.Repositorio.ListarRespostasAsync(id, Ct)).Count.ShouldBe(2);
    }
}
