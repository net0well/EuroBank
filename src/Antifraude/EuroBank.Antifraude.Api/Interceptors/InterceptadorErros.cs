using System.Data.Common;
using EuroBank.Shared.Exceptions;
using Grpc.Core;
using Grpc.Core.Interceptors;
using StackExchange.Redis;

namespace EuroBank.Antifraude.Api.Interceptors;

/// <summary>
/// Exceções viram status gRPC (e, no JSON transcoding, o status HTTP equivalente): validação InvalidArgument, não
/// encontrado NotFound, conflito Aborted, regra de negócio FailedPrecondition. Redis ou banco fora do ar viram
/// Unavailable: quem chama trata como antifraude indisponível (fail-closed), em vez de um Internal genérico.
/// </summary>
public sealed class InterceptadorErros(ILogger<InterceptadorErros> logger) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(request, context);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (ExcecaoDominio erro)
        {
            throw new RpcException(new Status(StatusDoDominio(erro), erro.Message));
        }
        catch (Exception erro) when (erro is RedisException or TimeoutException or DbException)
        {
            logger.LogWarning(erro, "Dependência indisponível em {Metodo}", context.Method);
            throw new RpcException(new Status(StatusCode.Unavailable, "Antifraude indisponível no momento; tente de novo."));
        }
    }

    private static StatusCode StatusDoDominio(ExcecaoDominio erro) => erro switch
    {
        ExcecaoValidacao => StatusCode.InvalidArgument,
        ExcecaoNaoEncontrado => StatusCode.NotFound,
        ExcecaoConflito or ExcecaoConcorrencia => StatusCode.Aborted,
        ExcecaoRegraNegocio => StatusCode.FailedPrecondition,
        _ => StatusCode.InvalidArgument,
    };
}
