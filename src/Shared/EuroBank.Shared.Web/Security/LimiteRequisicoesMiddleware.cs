using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace EuroBank.Shared.Web.Security;

/// <summary>
/// Rate limit por API key, em janela deslizante de 1 s guardada no Redis (um sorted set por API e key, mexido por um
/// script Lua atômico): várias instâncias da mesma API dividem o mesmo limite. Acima dele, 429 com Retry-After. Com o
/// Redis fora, deixa passar (fail-open): o limite protege a API, não pode derrubá-la. /health, Swagger e gRPC ficam de
/// fora.
/// </summary>
public sealed class LimiteRequisicoesMiddleware(
    RequestDelegate proximo,
    IOptions<OpcoesLimiteRequisicoes> opcoes,
    IServiceProvider servicos,
    IConfiguration configuracao,
    IHostEnvironment ambiente,
    TimeProvider relogio,
    IProblemDetailsService problemDetails,
    ILogger<LimiteRequisicoesMiddleware> logger)
{
    private static readonly TimeSpan Janela = TimeSpan.FromSeconds(1);

    private const string Script = """
        redis.call('ZREMRANGEBYSCORE', KEYS[1], '-inf', tonumber(ARGV[1]) - tonumber(ARGV[2]))
        if redis.call('ZCARD', KEYS[1]) < tonumber(ARGV[3]) then
          redis.call('ZADD', KEYS[1], ARGV[1], ARGV[4])
          redis.call('PEXPIRE', KEYS[1], ARGV[2])
          return 0
        end
        local maisAntiga = redis.call('ZRANGE', KEYS[1], 0, 0, 'WITHSCORES')
        return tonumber(maisAntiga[2]) + tonumber(ARGV[2]) - tonumber(ARGV[1])
        """;

    // Sem ConnectionStrings:Redis, o limite fica desligado (nem chega a criar a conexão).
    private readonly Lazy<IConnectionMultiplexer?> _redis = new(() =>
        string.IsNullOrWhiteSpace(configuracao.GetConnectionString("Redis")) ? null : servicos.GetService<IConnectionMultiplexer>());

    public async Task InvokeAsync(HttpContext contexto)
    {
        if (!opcoes.Value.Ativo
            || ForaDoLimite(contexto.Request)
            || !contexto.Request.Headers.TryGetValue(OpcoesSeguranca.HeaderApiKey, out var apiKey)
            || _redis.Value is not { } redis)
        {
            await proximo(contexto);
            return;
        }

        long esperaMs;
        try
        {
            var agora = relogio.GetUtcNow().ToUnixTimeMilliseconds();
            esperaMs = (long)await redis.GetDatabase().ScriptEvaluateAsync(
                Script,
                [Chave(apiKey.ToString())],
                [agora, (long)Janela.TotalMilliseconds, opcoes.Value.PorSegundo, $"{agora}:{Guid.NewGuid():N}"]);
        }
        catch (Exception erro) when (erro is RedisException or RedisTimeoutException)
        {
            logger.LogDebug(erro, "Redis indisponível: requisição liberada sem conferir o limite");
            await proximo(contexto);
            return;
        }

        if (esperaMs <= 0)
        {
            await proximo(contexto);
            return;
        }

        var segundos = Math.Max(1, (int)Math.Ceiling(esperaMs / 1000d));
        contexto.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        contexto.Response.Headers.RetryAfter = segundos.ToString(CultureInfo.InvariantCulture);
        await problemDetails.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = contexto,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Limite de requisições excedido",
                Detail = $"Esta API key passou de {opcoes.Value.PorSegundo} requisições por segundo; repita em {segundos} s.",
                Type = "https://eurobank.dev/erros/limite_excedido",
                Extensions = { ["codigo"] = "limite_excedido" },
            },
        });
    }

    private static bool ForaDoLimite(HttpRequest requisicao) =>
        requisicao.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase)
        || requisicao.Path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase)
        || (requisicao.ContentType?.StartsWith("application/grpc", StringComparison.OrdinalIgnoreCase) ?? false);

    /// <summary>Por API e por key; a key entra como hash, para não ficar legível no Redis.</summary>
    private RedisKey Chave(string apiKey)
    {
        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(apiKey)))[..16];
        return $"limite:{ambiente.ApplicationName}:{hash}";
    }
}
