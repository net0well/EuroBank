using EuroBank.Antifraude.Application.Interfaces;
using EuroBank.Antifraude.Domain.Repositories;
using EuroBank.Antifraude.Infrastructure.Outbox;
using EuroBank.Antifraude.Infrastructure.Persistence;
using EuroBank.Antifraude.Infrastructure.Persistence.Queries;
using EuroBank.Antifraude.Infrastructure.Persistence.Repositories;
using EuroBank.Antifraude.Infrastructure.Persistence.Seed;
using EuroBank.Shared.Domain;
using EuroBank.Shared.Persistence.Outbox;
using EuroBank.Shared.Persistence.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EuroBank.Antifraude.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AdicionarInfraestruturaAntifraude(this IServiceCollection services)
    {
        services.AddOptions<OpcoesOutbox>().BindConfiguration(OpcoesOutbox.Secao);
        services.AddSingleton<IMapeadorMensagensOutbox, MapeadorMensagensOutbox>();

        // A connection string é lida quando o DbContext é criado, para os testes poderem sobrescrevê-la.
        services.AddDbContext<AntifraudeDbContext>((provedor, opcoes) => opcoes
            .UseNpgsql(provedor.GetRequiredService<IConfiguration>().GetConnectionString("Antifraude")
                ?? throw new InvalidOperationException("ConnectionStrings:Antifraude não configurada.")));

        services.AddScoped<IRepositorioAnalises, RepositorioAnalises>();
        services.AddScoped<IRepositorioCasos, RepositorioCasos>();
        services.AddScoped<IRepositorioRegras, RepositorioRegras>();
        services.AddScoped<IRepositorioConfiguracao, RepositorioConfiguracao>();
        services.AddScoped<IRepositorioListas, RepositorioListas>();
        services.AddScoped<IRepositorioFraudes, RepositorioFraudes>();
        services.AddScoped<IConsultaAntifraude, ConsultaAntifraude>();
        services.AddScoped<SemeadorAntifraude>();
        services.AddScoped<IFilaOutbox, FilaOutbox>();
        services.AddScoped<IUnidadeDeTrabalho, UnidadeDeTrabalhoComOutbox<AntifraudeDbContext>>();
        services.AddScoped<IExecutorTransacional, ExecutorTransacionalEf<AntifraudeDbContext>>();
        return services;
    }
}
