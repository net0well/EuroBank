using EuroBank.Antifraude.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace EuroBank.Antifraude.Infrastructure.Perfil;

public static class RedisExtensions
{
    /// <summary>
    /// Conexão única com o Redis (o multiplexer é feito para ser compartilhado) e o perfil das contas. Com
    /// abortConnect=false a aplicação sobe mesmo com o Redis fora; com o backlog em fail-fast, as operações falham na
    /// hora até ele voltar.
    /// </summary>
    public static IServiceCollection AdicionarPerfilRedis(this IServiceCollection services)
    {
        services.TryAddSingleton<IConnectionMultiplexer>(provedor =>
        {
            var configuracao = provedor.GetRequiredService<IConfiguration>().GetConnectionString("Redis")
                ?? throw new InvalidOperationException("ConnectionStrings:Redis não configurada.");
            var opcoes = ConfigurationOptions.Parse(configuracao);
            opcoes.AbortOnConnectFail = false;
            opcoes.BacklogPolicy = BacklogPolicy.FailFast;
            return ConnectionMultiplexer.Connect(opcoes);
        });
        services.AddSingleton<IPerfisContas, PerfisContasRedis>();
        return services;
    }
}
