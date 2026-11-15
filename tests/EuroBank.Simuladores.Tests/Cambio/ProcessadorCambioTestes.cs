using System.Text.Json;
using EuroBank.Simuladores.Cambio;
using EuroBank.Simuladores.Cambio.Persistencia;
using EuroBank.Simuladores.Cambio.Regras;
using EuroBank.Simuladores.Comum;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using StackExchange.Redis;

namespace EuroBank.Simuladores.Tests.Cambio;

public sealed class ProcessadorCambioTestes(CambioFixture banco) : IClassFixture<CambioFixture>
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 9, 12, 0, 0, TimeSpan.Zero);

    private static readonly IOptions<OpcoesCambio> Opcoes =
        Options.Create(new OpcoesCambio { LatenciaMs = 500, ValidadeMinutos = 10, Spread = 0.01m });

    private readonly FakeTimeProvider _relogio = new(Agora);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private ProcessadorComandosCambio Processador(IConnectionMultiplexer? redis = null) => new(
        banco.Repositorio,
        new CotacoesValidas(redis ?? banco.Redis, banco.Repositorio, NullLogger<CotacoesValidas>.Instance),
        new TabelaCambio(Opcoes),
        Opcoes,
        _relogio,
        NullLogger<ProcessadorComandosCambio>.Instance);

    private Task CotarAsync(Guid transferenciaId, string origem, string destino, string? idMensagem = null, ProcessadorComandosCambio? processador = null) =>
        (processador ?? Processador()).ProcessarAsync(
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

    private async Task<List<Guid>> CotacoesRespondidasAsync(Guid transferenciaId) =>
        (await banco.Repositorio.ListarRespostasAsync(transferenciaId, Ct))
        .Select(resposta =>
        {
            using var payload = JsonDocument.Parse(resposta.Payload);
            return payload.RootElement.GetProperty("cotacaoId").GetGuid();
        })
        .ToList();

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
        (await banco.Redis.GetDatabase().KeyTimeToLiveAsync($"cambio:cotacao:{id}")).ShouldNotBeNull()
            .ShouldBeInRange(TimeSpan.FromMinutes(9), TimeSpan.FromMinutes(10));
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
    public async Task MesmaMensagemSoUmaVez_EReenvioRecebeAMesmaCotacao()
    {
        var id = Guid.CreateVersion7();
        var idMensagem = Guid.CreateVersion7().ToString();

        await CotarAsync(id, "EUR", "USD", idMensagem);
        await CotarAsync(id, "EUR", "USD", idMensagem);
        _relogio.Advance(TimeSpan.FromSeconds(30));
        await CotarAsync(id, "EUR", "USD");

        (await banco.Repositorio.ListarCotacoesAsync(id, Ct)).ShouldHaveSingleItem();
        (await CotacoesRespondidasAsync(id)).ShouldSatisfyAllConditions(
            cotacoes => cotacoes.Count.ShouldBe(2),
            cotacoes => cotacoes.Distinct().ShouldHaveSingleItem());
    }

    [Fact]
    public async Task CotacaoVencida_EmiteOutra()
    {
        var id = Guid.CreateVersion7();
        await CotarAsync(id, "EUR", "USD");

        _relogio.Advance(TimeSpan.FromMinutes(11));
        await CotarAsync(id, "EUR", "USD");

        (await banco.Repositorio.ListarCotacoesAsync(id, Ct)).Count.ShouldBe(2);
        (await CotacoesRespondidasAsync(id)).Distinct().Count().ShouldBe(2);
    }

    [Fact]
    public async Task RedisFora_AValidaVemDaTabela()
    {
        var id = Guid.CreateVersion7();
        var semRedis = Processador(banco.RedisForaDoAr);

        await CotarAsync(id, "EUR", "USD", processador: semRedis);
        await CotarAsync(id, "EUR", "USD", processador: semRedis);

        (await banco.Repositorio.ListarCotacoesAsync(id, Ct)).ShouldHaveSingleItem();
        (await CotacoesRespondidasAsync(id)).Distinct().ShouldHaveSingleItem();
    }

    [Fact]
    public async Task ChaveSumiuDoRedis_AValidaVemDaTabela_EVoltaParaORedis()
    {
        var id = Guid.CreateVersion7();
        await CotarAsync(id, "EUR", "USD");
        await banco.Redis.GetDatabase().KeyDeleteAsync($"cambio:cotacao:{id}");

        await CotarAsync(id, "EUR", "USD");

        (await banco.Repositorio.ListarCotacoesAsync(id, Ct)).ShouldHaveSingleItem();
        (await banco.Redis.GetDatabase().KeyExistsAsync($"cambio:cotacao:{id}")).ShouldBeTrue();
    }
}
