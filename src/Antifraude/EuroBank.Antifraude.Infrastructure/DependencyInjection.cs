using EuroBank.Antifraude.Application.Interfaces;
using EuroBank.Antifraude.Domain.Repositories;
using EuroBank.Antifraude.Domain.Services;
using EuroBank.Antifraude.Infrastructure.MachineLearning;
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
        services.AddScoped<IRepositorioModelos, RepositorioModelos>();
        services.AddScoped<IRepositorioTreinos, RepositorioTreinos>();
        services.AddScoped<IRepositorioDrift, RepositorioDrift>();
        services.AddScoped<IConsultaAntifraude, ConsultaAntifraude>();
        services.AddScoped<IConsultaDrift, ConsultaDrift>();
        services.AddScoped<SemeadorAntifraude>();
        services.AddScoped<IFilaOutbox, FilaOutbox>();
        services.AddScoped<IUnidadeDeTrabalho, UnidadeDeTrabalhoComOutbox<AntifraudeDbContext>>();
        services.AddScoped<IExecutorTransacional, ExecutorTransacionalEf<AntifraudeDbContext>>();

        // Os modelos em memória: vazios até alguém os carregar (só a API carrega; veja AdicionarModelosEmProducao).
        services.AddMetrics();
        services.AddSingleton<TelemetriaModelo>();
        services.AddSingleton<PontuadorFraude>();
        services.AddSingleton<IPontuadorFraude>(provedor => provedor.GetRequiredService<PontuadorFraude>());
        return services;
    }

    /// <summary>
    /// Quem avalia operações carrega do Postgres a versão ativa e o challenger e os troca sem reiniciar (LISTEN no
    /// canal antifraude_modelos).
    /// </summary>
    public static IServiceCollection AdicionarModelosEmProducao(this IServiceCollection services)
    {
        services.AddSingleton<CarregadorModelos>();
        services.AddHostedService<OuvinteModelos>();
        return services;
    }
}
