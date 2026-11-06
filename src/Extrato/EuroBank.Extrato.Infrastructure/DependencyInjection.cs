using EuroBank.Extrato.Application.Interfaces;
using EuroBank.Extrato.Domain.Repositories;
using EuroBank.Extrato.Infrastructure.Persistence;
using EuroBank.Extrato.Infrastructure.Persistence.Queries;
using EuroBank.Extrato.Infrastructure.Persistence.Repositories;
using EuroBank.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EuroBank.Extrato.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AdicionarInfraestruturaExtrato(this IServiceCollection services)
    {
        // A connection string é lida quando o DbContext é criado, para os testes poderem sobrescrevê-la.
        services.AddDbContext<ExtratoDbContext>((provedor, opcoes) => opcoes
            .UseNpgsql(provedor.GetRequiredService<IConfiguration>().GetConnectionString("Extrato")
                ?? throw new InvalidOperationException("ConnectionStrings:Extrato não configurada.")));

        services.AddScoped<IRepositorioExtratos, RepositorioExtratos>();
        services.AddScoped<IUnidadeDeTrabalho, UnidadeDeTrabalho>();
        services.AddScoped<IConsultaExtratos, ConsultaExtratos>();
        return services;
    }
}
