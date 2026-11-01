using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace EuroBank.Contas.Api.Tests.Fixtures;

/// <summary>Atalhos HTTP para os testes de endpoints da API de Contas.</summary>
public sealed class ClienteContas(HttpClient http)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public HttpClient Http { get; } = http;

    public async Task<ContaResposta> AbrirAsync(string titular = "Titular de teste", decimal depositoInicial = 0m)
    {
        var resposta = await Http.PostAsJsonAsync("/contas", new { titular }, Ct);
        resposta.StatusCode.ShouldBe(HttpStatusCode.Created);
        var conta = (await resposta.Content.ReadFromJsonAsync<ContaResposta>(Ct))!;
        if (depositoInicial > 0)
        {
            conta = await LerContaAsync(await DepositarAsync(conta.Id, depositoInicial), HttpStatusCode.OK);
        }

        return conta;
    }

    public Task<HttpResponseMessage> DepositarAsync(Guid id, decimal valor) =>
        Http.PostAsJsonAsync($"/contas/{id}/depositos", new { valor }, Ct);

    public Task<HttpResponseMessage> SacarAsync(Guid id, decimal valor) =>
        Http.PostAsJsonAsync($"/contas/{id}/saques", new { valor }, Ct);

    public Task<HttpResponseMessage> TransferirAsync(Guid id, string ibanDestino, decimal valor) =>
        Http.PostAsJsonAsync($"/contas/{id}/transferencias", new { ibanDestino, valor }, Ct);

    public Task<HttpResponseMessage> ComandoAsync(Guid id, string recurso) =>
        Http.PostAsync($"/contas/{id}/{recurso}", content: null, Ct);

    public async Task<ContaResposta> ObterAsync(Guid id) =>
        (await Http.GetFromJsonAsync<ContaResposta>($"/contas/{id}", Ct))!;

    public static async Task<ContaResposta> LerContaAsync(HttpResponseMessage resposta, HttpStatusCode esperado)
    {
        resposta.StatusCode.ShouldBe(esperado, await resposta.Content.ReadAsStringAsync(Ct));
        return (await resposta.Content.ReadFromJsonAsync<ContaResposta>(Ct))!;
    }

    /// <summary>Confere status e o código de erro do ProblemDetails.</summary>
    public static async Task DeveSerProblemaAsync(HttpResponseMessage resposta, HttpStatusCode status, string codigo)
    {
        var corpo = await resposta.Content.ReadAsStringAsync(Ct);
        resposta.StatusCode.ShouldBe(status, corpo);
        resposta.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        using var problema = JsonDocument.Parse(corpo);
        problema.RootElement.GetProperty("status").GetInt32().ShouldBe((int)status);
        problema.RootElement.GetProperty("codigo").GetString().ShouldBe(codigo);
    }
}

public sealed record ContaResposta(
    Guid Id,
    string Iban,
    string Titular,
    decimal Saldo,
    string Moeda,
    string Status,
    int Versao,
    DateTimeOffset AbertaEm);
