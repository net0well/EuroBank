using EuroBank.Antifraude.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace EuroBank.Antifraude.Infrastructure.Perfil;

public static class RedisExtensions
{
    /// <summary>
    /// Conexão única com o Redis (o multiplexer é feito para ser compartilhado) e o perfil das contas. Com
    /// abortConnect=false a aplicação sobe mesmo com o Redis fora; as operações falham até ele voltar.
    /// </summary>
    public static IServiceCollection AdicionarPerfilRedis(this IServiceCollection services)
    {
        services.AddSingleton<IConnectionMultiplexer>(provedor =>
        {
            var configuracao = provedor.GetRequiredService<IConfiguration>().GetConnectionString("Redis")
                ?? throw new InvalidOperationException("ConnectionStrings:Redis não configurada.");
            var opcoes = ConfigurationOptions.Parse(configuracao);
            opcoes.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(opcoes);
        });
        services.AddSingleton<IPerfisContas, PerfisContasRedis>();
        return services;
    }
}
