using System.Security.Cryptography;
using System.Text;
using EuroBank.Shared.Web.Security;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Options;

namespace EuroBank.Antifraude.Api.Interceptors;

/// <summary>
/// Exige a API key na metadata (x-api-key) das chamadas ao AntifraudeService. Registrado só nesse serviço: health check
/// e reflection ficam abertos. Sem a key, ou com a key errada: Unauthenticated.
/// </summary>
public sealed class InterceptadorApiKey(IOptions<OpcoesSeguranca> opcoes) : Interceptor
{
    private readonly byte[] _chaveEsperada = Encoding.UTF8.GetBytes(opcoes.Value.ApiKey);

    public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var chave = context.RequestHeaders.GetValue("x-api-key");

        // Comparação em tempo constante, como no middleware HTTP.
        if (chave is null || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(chave), _chaveEsperada))
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, "API key ausente ou inválida (metadata x-api-key)."));
        }

        return continuation(request, context);
    }
}
