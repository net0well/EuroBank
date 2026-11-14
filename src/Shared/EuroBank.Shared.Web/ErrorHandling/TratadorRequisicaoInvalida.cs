using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace EuroBank.Shared.Web.ErrorHandling;

/// <summary>
/// Corpo ou parâmetro que não dá para ler (JSON mal formado, texto que não é UTF-8, número que não é número) é erro do
/// cliente: 400 ProblemDetails com o código requisicao_invalida. Em Development as minimal APIs lançam
/// BadHttpRequestException nesses casos, e sem este tratador a resposta seria 500.
/// </summary>
public sealed class TratadorRequisicaoInvalida(IProblemDetailsService problemDetails) : IExceptionHandler
{
    private const string Codigo = "requisicao_invalida";

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not BadHttpRequestException requisicaoInvalida)
        {
            return false;
        }

        httpContext.Response.StatusCode = requisicaoInvalida.StatusCode;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = requisicaoInvalida,
            ProblemDetails =
            {
                Status = requisicaoInvalida.StatusCode,
                Title = "Requisição inválida",
                Detail = "Não foi possível ler a requisição: confira o JSON do corpo (UTF-8) e os parâmetros.",
                Type = $"https://eurobank.dev/erros/{Codigo}",
                Extensions = { ["codigo"] = Codigo },
            },
        });
    }
}
