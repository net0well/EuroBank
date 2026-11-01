using EuroBank.Contas.Domain.Repositories;
using EuroBank.Contas.Domain.Services;
using EuroBank.Contas.Infrastructure.Outbox;
using EuroBank.Contas.Infrastructure.Persistence;
using EuroBank.Contas.Infrastructure.Persistence.Repositories;
using EuroBank.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EuroBank.Contas.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AdicionarInfraestruturaContas(
        this IServiceCollection services,
        IConfiguration configuracao)
    {
        var connectionString = configuracao.GetConnectionString("Contas")
            ?? throw new InvalidOperationException("ConnectionStrings:Contas não configurada.");

        services.AddOptions<OpcoesOutbox>().BindConfiguration(OpcoesOutbox.Secao);
        services.AddSingleton<MapeadorMensagensOutbox>();
        services.AddSingleton<InterceptorOutbox>();
        services.AddDbContext<ContasDbContext>((provedor, opcoes) => opcoes
            .UseNpgsql(connectionString)
            .AddInterceptors(provedor.GetRequiredService<InterceptorOutbox>()));

        services.AddScoped<IRepositorioContas, RepositorioContas>();
        services.AddScoped<IGeradorIban, GeradorIban>();
        services.AddScoped<IUnidadeDeTrabalho, UnidadeDeTrabalho>();
        return services;
    }
}
