using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace EuroBank.Shared.Web.Security;

/// <summary>
/// Exige o header X-Api-Key em todas as rotas de negócio. /health e o Swagger ficam abertos, para o healthcheck
/// do compose e a documentação funcionarem sem chave.
/// </summary>
public sealed class ValidacaoApiKeyMiddleware(
    RequestDelegate proximo,
    IOptions<OpcoesSeguranca> opcoes,
    IProblemDetailsService problemDetails)
{
    private readonly byte[] _chaveEsperada = Encoding.UTF8.GetBytes(opcoes.Value.ApiKey);

    public async Task InvokeAsync(HttpContext contexto)
    {
        if (RotaAberta(contexto.Request.Path) || ChaveValida(contexto.Request))
        {
            await proximo(contexto);
            return;
        }

        contexto.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await problemDetails.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = contexto,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "API key ausente ou inválida",
                Detail = $"Envie a API key no header {OpcoesSeguranca.HeaderApiKey}.",
                Type = "https://eurobank.dev/erros/api_key_invalida",
                Extensions = { ["codigo"] = "api_key_invalida" },
            },
        });
    }

    private static bool RotaAberta(PathString caminho) =>
        caminho.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase)
        || caminho.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase);

    // Comparação em tempo constante: não revela, pelo tempo de resposta, quantos caracteres da chave acertaram.
    private bool ChaveValida(HttpRequest requisicao) =>
        requisicao.Headers.TryGetValue(OpcoesSeguranca.HeaderApiKey, out var valor)
        && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(valor.ToString()), _chaveEsperada);
}
