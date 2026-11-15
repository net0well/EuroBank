using EuroBank.Shared.Web.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace EuroBank.Shared.Web.Extensions;

public static class LimiteRequisicoesExtensions
{
    /// <summary>
    /// Lê a seção LimiteRequisicoes e garante uma conexão com o Redis de ConnectionStrings:Redis (a do próprio serviço,
    /// se ele já registrou uma). Sem Redis configurado, o limite fica desligado.
    /// </summary>
    public static IServiceCollection AdicionarLimiteRequisicoes(this IServiceCollection services)
    {
        services.AddOptions<OpcoesLimiteRequisicoes>()
            .BindConfiguration(OpcoesLimiteRequisicoes.Secao)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.TryAddSingleton(TimeProvider.System);

        // Com abortConnect=false a API sobe com o Redis fora; com fail-fast, a conferência falha na hora e a requisição
        // passa, em vez de esperar o timeout.
        services.TryAddSingleton<IConnectionMultiplexer>(provedor =>
        {
            var opcoes = ConfigurationOptions.Parse(provedor.GetRequiredService<IConfiguration>().GetConnectionString("Redis")
                ?? throw new InvalidOperationException("ConnectionStrings:Redis não configurada."));
            opcoes.AbortOnConnectFail = false;
            opcoes.BacklogPolicy = BacklogPolicy.FailFast;
            return ConnectionMultiplexer.Connect(opcoes);
        });

        return services;
    }

    /// <summary>Depois da API key: só requisição autenticada conta para o limite.</summary>
    public static IApplicationBuilder UsarLimiteRequisicoes(this IApplicationBuilder app) =>
        app.UseMiddleware<LimiteRequisicoesMiddleware>();
}
