using System.Net;

namespace EuroBank.Web.Antifraude.Clients;

/// <summary>A REST do antifraude recusou o pedido: o status, o código do ProblemDetails e a mensagem para o analista.</summary>
public sealed class ExcecaoApiAntifraude(HttpStatusCode status, string? codigo, string mensagem) : Exception(mensagem)
{
    public HttpStatusCode Status { get; } = status;

    public string? Codigo { get; } = codigo;
}
