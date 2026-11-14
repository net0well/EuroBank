using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace EuroBank.Antifraude.Api.Tests.Fixtures;

/// <summary>Atalhos do REST do analista e das respostas de erro (ProblemDetails com código).</summary>
public sealed class ClienteAnalista(AntifraudeFixture antifraude) : IDisposable
{
    private readonly HttpClient _http = antifraude.CriarClienteHttp();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public HttpClient Http => _http;

    /// <summary>Uma operação que vai para revisão (score 50) numa conta com histórico; devolve o id do caso.</summary>
    public async Task<(Guid CasoId, Guid OperacaoId)> CasoPendenteAsync(string iban = CenarioAvaliacao.IbanConhecido)
    {
        var conta = await new CenarioAvaliacao(antifraude).ContaComHistoricoAsync();
        var resultado = await antifraude.CriarClienteGrpc()
            .AvaliarAsync(CenarioAvaliacao.Pedido(conta, 4_600m, iban), cancellationToken: Ct);
        return (Guid.Parse(resultado.CasoId), Guid.Parse(resultado.OperacaoId));
    }

    public Task<HttpResponseMessage> PostAsync(string rota, object corpo) => _http.PostAsJsonAsync(rota, corpo, Ct);

    public Task<HttpResponseMessage> PutAsync(string rota, object corpo) => _http.PutAsJsonAsync(rota, corpo, Ct);

    public Task<HttpResponseMessage> DeleteAsync(string rota) => _http.DeleteAsync(rota, Ct);

    public Task<JsonElement> GetAsync(string rota) => _http.GetFromJsonAsync<JsonElement>(rota, Ct);

    public static async Task<JsonElement> OkAsync(HttpResponseMessage resposta)
    {
        var corpo = await resposta.Content.ReadAsStringAsync(Ct);
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK, corpo);
        return JsonDocument.Parse(corpo).RootElement.Clone();
    }

    public static async Task DeveSerProblemaAsync(HttpResponseMessage resposta, HttpStatusCode status, string codigo)
    {
        var corpo = await resposta.Content.ReadAsStringAsync(Ct);
        resposta.StatusCode.ShouldBe(status, corpo);
        using var problema = JsonDocument.Parse(corpo);
        problema.RootElement.GetProperty("codigo").GetString().ShouldBe(codigo);
    }

    /// <summary>Um IBAN alemão que nenhuma outra avaliação usa (para bloquear sem afetar os outros testes).</summary>
    public static string IbanUnico() => $"DE89{Random.Shared.NextInt64(1_000_000_000_000_000, 9_999_999_999_999_999):D18}";

    public void Dispose() => _http.Dispose();
}
