using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace EuroBank.Antifraude.Worker.Health;

/// <summary>O worker só serve se o Redis responder: é lá que o perfil das contas mora.</summary>
public sealed class VerificacaoRedis(IConnectionMultiplexer redis) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var latencia = await redis.GetDatabase().PingAsync();
            return HealthCheckResult.Healthy($"Redis respondeu em {latencia.TotalMilliseconds:0} ms");
        }
        catch (RedisException erro)
        {
            return HealthCheckResult.Unhealthy("Redis fora do ar", erro);
        }
    }
}
