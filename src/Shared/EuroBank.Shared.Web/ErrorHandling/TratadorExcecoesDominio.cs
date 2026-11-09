using EuroBank.Shared.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace EuroBank.Shared.Web.ErrorHandling;

/// <summary>
/// Converte exceções de domínio em ProblemDetails (RFC 9457): validação 400, não encontrado 404,
/// concorrência ou conflito 409 e regra de negócio 422. Outras exceções seguem para o tratamento padrão (500).
/// </summary>
public sealed class TratadorExcecoesDominio(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if ((exception as ExcecaoDominio ?? exception.InnerException as ExcecaoDominio) is not { } excecao)
        {
            return false;
        }

        var (status, titulo) = excecao switch
        {
            ExcecaoValidacao => (StatusCodes.Status400BadRequest, "Requisição inválida"),
            ExcecaoNaoEncontrado => (StatusCodes.Status404NotFound, "Recurso não encontrado"),
            ExcecaoConcorrencia => (StatusCodes.Status409Conflict, "Conflito de concorrência"),
            ExcecaoConflito => (StatusCodes.Status409Conflict, "Operação não é mais possível"),
            ExcecaoRegraNegocio => (StatusCodes.Status422UnprocessableEntity, "Regra de negócio violada"),
            _ => (StatusCodes.Status400BadRequest, "Requisição inválida"),
        };

        httpContext.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = excecao,
            ProblemDetails =
            {
                Status = status,
                Title = titulo,
                Detail = excecao.Message,
                Type = $"https://eurobank.dev/erros/{excecao.Codigo}",
                Extensions = { ["codigo"] = excecao.Codigo },
            },
        });
    }
}
