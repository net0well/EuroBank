using EuroBank.Shared.Domain;
using EuroBank.Shared.Persistence.Outbox;
using EuroBank.Shared.Persistence.Transactions;
using EuroBank.Transferencias.Application.Interfaces;
using EuroBank.Transferencias.Domain.Repositories;
using EuroBank.Transferencias.Infrastructure.Options;
using EuroBank.Transferencias.Infrastructure.Outbox;
using EuroBank.Transferencias.Infrastructure.Persistence;
using EuroBank.Transferencias.Infrastructure.Persistence.Queries;
using EuroBank.Transferencias.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EuroBank.Transferencias.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AdicionarInfraestruturaTransferencias(this IServiceCollection services)
    {
        services.AddOptions<OpcoesOutbox>().BindConfiguration(OpcoesOutbox.Secao);
        services.AddOptions<OpcoesSaga>().BindConfiguration(OpcoesSaga.Secao);
        services.AddSingleton(provedor => provedor.GetRequiredService<IOptions<OpcoesSaga>>().Value.ParaPolitica());
        services.AddOptions<OpcoesSepa>().BindConfiguration(OpcoesSepa.Secao);
        services.AddSingleton(provedor => provedor.GetRequiredService<IOptions<OpcoesSepa>>().Value.ParaPolitica());
        services.AddSingleton<IMapeadorMensagensOutbox, MapeadorMensagensOutbox>();

        // A connection string é lida quando o DbContext é criado, para os testes poderem sobrescrevê-la.
        services.AddDbContext<TransferenciasDbContext>((provedor, opcoes) => opcoes
            .UseNpgsql(provedor.GetRequiredService<IConfiguration>().GetConnectionString("Transferencias")
                ?? throw new InvalidOperationException("ConnectionStrings:Transferencias não configurada.")));

        services.AddScoped<IRepositorioTransferencias, RepositorioTransferencias>();
        services.AddScoped<IConsultaTransferencias, ConsultaTransferencias>();
        services.AddScoped<IContasConhecidas, ContasConhecidas>();
        services.AddScoped<IFilaOutbox, FilaOutbox>();
        services.AddScoped<IUnidadeDeTrabalho, UnidadeDeTrabalhoComOutbox<TransferenciasDbContext>>();
        services.AddScoped<IExecutorTransacional, ExecutorTransacionalEf<TransferenciasDbContext>>();
        return services;
    }
}
