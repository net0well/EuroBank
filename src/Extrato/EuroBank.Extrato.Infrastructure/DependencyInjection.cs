using EuroBank.Extrato.Application.Interfaces;
using EuroBank.Extrato.Domain.Repositories;
using EuroBank.Extrato.Infrastructure.Cache;
using EuroBank.Extrato.Infrastructure.Cache.WriteBehind;
using EuroBank.Extrato.Infrastructure.Enums;
using EuroBank.Extrato.Infrastructure.Options;
using EuroBank.Extrato.Infrastructure.Persistence;
using EuroBank.Extrato.Infrastructure.Persistence.Queries;
using EuroBank.Extrato.Infrastructure.Persistence.Repositories;
using EuroBank.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace EuroBank.Extrato.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AdicionarInfraestruturaExtrato(this IServiceCollection services)
    {
        // A connection string é lida quando o DbContext é criado, para os testes poderem sobrescrevê-la.
        services.AddDbContext<ExtratoDbContext>((provedor, opcoes) => opcoes
            .UseNpgsql(provedor.GetRequiredService<IConfiguration>().GetConnectionString("Extrato")
                ?? throw new InvalidOperationException("ConnectionStrings:Extrato não configurada.")));

        services.AddScoped<RepositorioExtratos>();
        services.AddScoped<UnidadeDeTrabalho>();
        services.AddScoped<ConsultaExtratos>();
        services.AdicionarCacheExtrato();
        return services;
    }

    /// <summary>
    /// O cache do extrato conforme Cache:Estrategia: as interfaces da aplicação resolvem para o Postgres puro, para os
    /// decoradores do cache-aside e do write-through ou para o repositório e a unidade de trabalho do write-behind.
    /// </summary>
    private static void AdicionarCacheExtrato(this IServiceCollection services)
    {
        services.AddOptions<OpcoesCache>().BindConfiguration(OpcoesCache.Secao);
        services.TryAddSingleton(TimeProvider.System);
        services.AddMetrics();

        // Uma conexão só (o multiplexer é feito para ser compartilhado). Com abortConnect=false a aplicação sobe com o
        // Redis fora; com o backlog em fail-fast, cada operação falha na hora (sem esperar o timeout) até ele voltar, e o
        // cache cai para o Postgres.
        services.TryAddSingleton<IConnectionMultiplexer>(provedor =>
        {
            var configuracao = provedor.GetRequiredService<IConfiguration>().GetConnectionString("Redis")
                ?? throw new InvalidOperationException("ConnectionStrings:Redis não configurada.");
            var opcoes = ConfigurationOptions.Parse(configuracao);
            opcoes.AbortOnConnectFail = false;
            opcoes.BacklogPolicy = BacklogPolicy.FailFast;
            return ConnectionMultiplexer.Connect(opcoes);
        });

        services.AddSingleton<CacheExtratos>();
        services.AddSingleton<MarcaCacheSuspeito>();
        services.AddSingleton<MetricasCache>();
        services.AddSingleton<ReconstrucaoCache>();
        services.AddSingleton<DescargaPendencias>();
        services.AddScoped<ConsultaExtratosComCache>();
        services.AddScoped<UnidadeDeTrabalhoComCache>();
        services.AddScoped<SessaoWriteBehind>();
        services.AddScoped<RepositorioExtratosWriteBehind>();
        services.AddScoped<UnidadeDeTrabalhoWriteBehind>();
        services.AddScoped<GravadorExtratosPostgres>();

        services.AddScoped<IConsultaExtratos>(provedor => Estrategia(provedor) == EstrategiaCache.Nenhuma
            ? provedor.GetRequiredService<ConsultaExtratos>()
            : provedor.GetRequiredService<ConsultaExtratosComCache>());

        services.AddScoped<IRepositorioExtratos>(provedor => Estrategia(provedor) == EstrategiaCache.WriteBehind
            ? provedor.GetRequiredService<RepositorioExtratosWriteBehind>()
            : provedor.GetRequiredService<RepositorioExtratos>());

        services.AddScoped<IUnidadeDeTrabalho>(provedor => Estrategia(provedor) switch
        {
            EstrategiaCache.Nenhuma => provedor.GetRequiredService<UnidadeDeTrabalho>(),
            EstrategiaCache.WriteBehind => provedor.GetRequiredService<UnidadeDeTrabalhoWriteBehind>(),
            _ => provedor.GetRequiredService<UnidadeDeTrabalhoComCache>(),
        });
    }

    private static EstrategiaCache Estrategia(IServiceProvider provedor) =>
        provedor.GetRequiredService<IOptions<OpcoesCache>>().Value.Estrategia;
}
