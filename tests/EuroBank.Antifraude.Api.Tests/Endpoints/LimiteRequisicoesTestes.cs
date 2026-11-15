using System.Net;
using EuroBank.Antifraude.Api.Tests.Fixtures;
using static EuroBank.Antifraude.Api.Tests.Fixtures.CenarioAvaliacao;

namespace EuroBank.Antifraude.Api.Tests.Endpoints;

/// <summary>
/// O rate limit compartilhado das APIs (Shared.Web), exercitado na REST do antifraude: janela deslizante no Redis, 429
/// com Retry-After, /health e gRPC de fora, e fail-open com o Redis fora do ar.
/// </summary>
public sealed class LimiteRequisicoesTestes(AntifraudeFixture antifraude)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Uma key só deste teste: o limite é por API e por key, e os outros testes usam a padrão.</summary>
    private static string KeyPropria() => $"chave-limite-{Guid.CreateVersion7():N}";

    [Fact]
    public async Task AcimaDoLimite_Responde429ComRetryAfter()
    {
        var apiKey = KeyPropria();
        await using var api = antifraude.CriarApi(antifraude.ConnectionStringRedis, limitePorSegundo: 5, apiKey);
        using var http = api.CreateClient();
        http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

        var respostas = new List<HttpResponseMessage>();
        for (var i = 0; i < 8; i++)
        {
            respostas.Add(await http.GetAsync("/regras", Ct));
        }

        respostas.Take(5).ShouldAllBe(resposta => resposta.StatusCode == HttpStatusCode.OK);
        var barrada = respostas[^1];
        barrada.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        barrada.Headers.RetryAfter!.Delta.ShouldBe(TimeSpan.FromSeconds(1));
        (await barrada.Content.ReadAsStringAsync(Ct)).ShouldContain("limite_excedido");

        // A janela anda: passado o segundo, a mesma key volta a ser atendida.
        await Task.Delay(TimeSpan.FromSeconds(1.1), Ct);
        (await http.GetAsync("/regras", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthEGrpc_NaoContam()
    {
        var apiKey = KeyPropria();
        await using var api = antifraude.CriarApi(antifraude.ConnectionStringRedis, limitePorSegundo: 2, apiKey);
        using var http = api.CreateClient();
        var grpc = antifraude.CriarClienteGrpc(apiKey, api);

        for (var i = 0; i < 6; i++)
        {
            (await http.GetAsync("/health", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
            (await grpc.SimularAsync(Pedido(Guid.CreateVersion7()), cancellationToken: Ct)).Simulacao.ShouldBeTrue();
        }
    }

    [Fact]
    public async Task RedisFora_DeixaPassar()
    {
        var apiKey = KeyPropria();
        await using var api = antifraude.CriarApi("127.0.0.1:1,connectTimeout=300", limitePorSegundo: 1, apiKey);
        using var http = api.CreateClient();
        http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

        for (var i = 0; i < 5; i++)
        {
            (await http.GetAsync("/regras", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        }
    }
}
