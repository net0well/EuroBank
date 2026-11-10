using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace EuroBank.Transferencias.Api.Tests.Fixtures;

/// <summary>Atalhos HTTP para os testes da API de Transferências.</summary>
public sealed class ClienteTransferencias(HttpClient http)
{
    public const string IbanComum = "DE89370400440532013000";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public static object Pedido(
        Guid? contaOrigemId = null,
        decimal valor = 100m,
        string iban = IbanComum,
        string? bic = "DEUTDEFF",
        DateTimeOffset? executarEm = null,
        string? moedaDestino = null,
        string? moedaDoValor = null) => new
        {
            contaOrigemId = contaOrigemId ?? PostgresFixture.ContaPadrao,
            ibanDestino = iban,
            bic,
            nomeBeneficiario = "Bruno Externo",
            valor,
            executarEm,
            descricao = "Aluguel",
            moedaDestino,
            moedaDoValor,
        };

    public static object PedidoCambio(Guid contaOrigemId, string ibanDestino, decimal valor = 100m, string? moedaDoValor = null) => new
    {
        contaOrigemId,
        ibanDestino,
        valor,
        moedaDoValor,
    };

    public Task<HttpResponseMessage> SolicitarCambioAsync(object pedido) =>
        Enviar("/transferencias-cambio", pedido, Guid.CreateVersion7().ToString());

    public Task<HttpResponseMessage> SolicitarAsync(object pedido, string? idempotencyKey) =>
        Enviar("/transferencias-externas", pedido, idempotencyKey);

    private Task<HttpResponseMessage> Enviar(string rota, object pedido, string? idempotencyKey)
    {
        var requisicao = new HttpRequestMessage(HttpMethod.Post, rota) { Content = JsonContent.Create(pedido) };
        if (idempotencyKey is not null)
        {
            requisicao.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        return http.SendAsync(requisicao, Ct);
    }

    public async Task<TransferenciaResposta> SolicitarComSucessoAsync(object pedido) =>
        await LerAsync(await SolicitarAsync(pedido, Guid.CreateVersion7().ToString()), HttpStatusCode.Accepted);

    public async Task<TransferenciaResposta> ObterAsync(Guid id) =>
        (await http.GetFromJsonAsync<TransferenciaResposta>($"/transferencias/{id}", Ct))!;

    public Task<HttpResponseMessage> CancelarAsync(Guid id) =>
        http.PostAsync($"/transferencias/{id}/cancelamento", content: null, Ct);

    public Task<HttpResponseMessage> GetAsync(string url) => http.GetAsync(url, Ct);

    public static async Task<TransferenciaResposta> LerAsync(HttpResponseMessage resposta, HttpStatusCode esperado)
    {
        resposta.StatusCode.ShouldBe(esperado, await resposta.Content.ReadAsStringAsync(Ct));
        return (await resposta.Content.ReadFromJsonAsync<TransferenciaResposta>(Ct))!;
    }

    /// <summary>Confere status e o código de erro do ProblemDetails.</summary>
    public static async Task DeveSerProblemaAsync(HttpResponseMessage resposta, HttpStatusCode status, string codigo)
    {
        var corpo = await resposta.Content.ReadAsStringAsync(Ct);
        resposta.StatusCode.ShouldBe(status, corpo);
        using var problema = JsonDocument.Parse(corpo);
        problema.RootElement.GetProperty("codigo").GetString().ShouldBe(codigo);
    }
}

public sealed record TransferenciaResposta(
    Guid Id,
    string Tipo,
    Guid ContaOrigemId,
    string IbanDestino,
    string Bic,
    string NomeBeneficiario,
    decimal Valor,
    string MoedaDoValor,
    decimal Tarifa,
    string MoedaOrigem,
    decimal? ValorDebitado,
    decimal? ValorDestino,
    string MoedaDestino,
    CotacaoResposta? Cotacao,
    DateTimeOffset? ExecutarEm,
    DateTimeOffset ExecutarEmEfetivo,
    string Estado,
    string? Motivo,
    bool CancelamentoSolicitado,
    int TentativasNoPasso,
    DateTimeOffset? PrazoDoPasso,
    int Versao,
    IReadOnlyList<PassoResposta> Historico);

public sealed record CotacaoResposta(Guid Id, decimal Taxa, DateTimeOffset ValidaAte);

public sealed record PassoResposta(int Ordem, string EstadoApos, string Descricao, DateTimeOffset OcorridoEm);
