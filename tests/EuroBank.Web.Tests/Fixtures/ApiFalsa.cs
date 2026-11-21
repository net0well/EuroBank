using System.Net;
using System.Text;
using System.Text.Json;

namespace EuroBank.Web.Tests.Fixtures;

/// <summary>
/// A REST do antifraude de mentira: cada rota ("GET casos", "POST casos/{id}/atribuicao") devolve o que o teste
/// mandou, e cada pedido fica guardado (método, caminho com a query, corpo). Rota sem resposta: 404.
/// </summary>
public sealed class ApiFalsa : HttpMessageHandler
{
    public static readonly Uri Endereco = new("http://antifraude.teste/");

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly Dictionary<string, Func<HttpResponseMessage>> _respostas = new(StringComparer.Ordinal);
    private readonly List<PedidoRecebido> _pedidos = [];

    public IReadOnlyList<PedidoRecebido> Pedidos
    {
        get
        {
            lock (_pedidos)
            {
                return [.. _pedidos];
            }
        }
    }

    /// <summary>Responde 200 com o objeto em JSON (a rota sem a query: "GET casos").</summary>
    public ApiFalsa Responder(string metodoERota, object corpo) =>
        Responder(metodoERota, () => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(corpo, Json), Encoding.UTF8, "application/json"),
        });

    /// <summary>Responde 200 com o que a função devolver na hora do pedido (para o teste mudar a resposta depois).</summary>
    public ApiFalsa ResponderCom(string metodoERota, Func<object> corpoAtual) =>
        Responder(metodoERota, () => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(corpoAtual(), Json), Encoding.UTF8, "application/json"),
        });

    public ApiFalsa Responder(string metodoERota, Func<HttpResponseMessage> resposta)
    {
        _respostas[metodoERota] = resposta;
        return this;
    }

    public ApiFalsa ResponderProblema(string metodoERota, HttpStatusCode status, string codigo, string detalhe) =>
        Responder(metodoERota, () => new HttpResponseMessage(status)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { title = "Erro", status = (int)status, detail = detalhe, codigo }, Json),
                Encoding.UTF8,
                "application/problem+json"),
        });

    public HttpClient CriarCliente() => new(this) { BaseAddress = Endereco };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var caminho = request.RequestUri!.PathAndQuery.TrimStart('/');
        var corpo = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        lock (_pedidos)
        {
            _pedidos.Add(new PedidoRecebido(request.Method.Method, caminho, corpo));
        }

        var rota = $"{request.Method.Method} {caminho.Split('?')[0]}";
        return _respostas.TryGetValue(rota, out var resposta) ? resposta() : new HttpResponseMessage(HttpStatusCode.NotFound);
    }
}
