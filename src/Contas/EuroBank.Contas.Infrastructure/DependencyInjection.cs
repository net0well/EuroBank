using EuroBank.Antifraude.Grpc;
using EuroBank.Contas.Application.Interfaces;
using EuroBank.Contas.Domain.Repositories;
using EuroBank.Contas.Domain.Services;
using EuroBank.Contas.Infrastructure.Clients;
using EuroBank.Contas.Infrastructure.Options;
using EuroBank.Contas.Infrastructure.Outbox;
using EuroBank.Contas.Infrastructure.Persistence;
using EuroBank.Contas.Infrastructure.Persistence.Queries;
using EuroBank.Contas.Infrastructure.Persistence.Repositories;
using EuroBank.Shared.Domain;
using EuroBank.Shared.Persistence.Outbox;
using Grpc.Core;
using Grpc.Net.Client.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EuroBank.Contas.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AdicionarInfraestruturaContas(this IServiceCollection services)
    {
        services.AddOptions<OpcoesOutbox>().BindConfiguration(OpcoesOutbox.Secao);
        services.AddSingleton<IMapeadorMensagensOutbox, MapeadorMensagensOutbox>();

        // A connection string é lida quando o DbContext é criado, para os testes poderem sobrescrevê-la.
        services.AddDbContext<ContasDbContext>((provedor, opcoes) => opcoes
            .UseNpgsql(provedor.GetRequiredService<IConfiguration>().GetConnectionString("Contas")
                ?? throw new InvalidOperationException("ConnectionStrings:Contas não configurada.")));

        services.AddScoped<IRepositorioContas, RepositorioContas>();
        services.AddScoped<IGeradorIban, GeradorIban>();
        services.AddScoped<IFilaOutbox, FilaOutbox>();
        services.AddScoped<IUnidadeDeTrabalho, UnidadeDeTrabalhoComOutbox<ContasDbContext>>();
        services.AddScoped<IRespostasSaga, RespostasSagaOutbox>();
        services.AddScoped<IConsultaContas, ConsultaContas>();
        services.AdicionarClienteAntifraude();
        return services;
    }

    /// <summary>
    /// O cliente gRPC do antifraude, com retry em Unavailable dentro do prazo e a API key como credencial. Com
    /// Antifraude:Ativo desligado, a análise é a que aprova tudo (lido a cada pedido, para os testes poderem mudar).
    /// </summary>
    private static void AdicionarClienteAntifraude(this IServiceCollection services)
    {
        services.AddOptions<OpcoesAntifraude>().BindConfiguration(OpcoesAntifraude.Secao);
        services
            .AddGrpcClient<AntifraudeService.AntifraudeServiceClient>(OpcoesAntifraude.NomeCliente, (provedor, cliente) =>
                cliente.Address = provedor.GetRequiredService<IOptions<OpcoesAntifraude>>().Value.Endereco)
            .ConfigureChannel(canal =>
            {
                // No laboratório o canal é h2c (sem TLS): sem esta opção, a API key não iria na chamada.
                canal.UnsafeUseInsecureChannelCallCredentials = true;
                canal.ServiceConfig = new ServiceConfig
                {
                    MethodConfigs =
                    {
                        new MethodConfig
                        {
                            Names = { MethodName.Default },
                            RetryPolicy = new RetryPolicy
                            {
                                MaxAttempts = 3,
                                InitialBackoff = TimeSpan.FromMilliseconds(100),
                                MaxBackoff = TimeSpan.FromSeconds(1),
                                BackoffMultiplier = 2,
                                RetryableStatusCodes = { StatusCode.Unavailable },
                            },
                        },
                    },
                };
            })
            .AddCallCredentials((_, metadata, provedor) =>
            {
                metadata.Add("x-api-key", provedor.GetRequiredService<IOptions<OpcoesAntifraude>>().Value.ApiKey);
                return Task.CompletedTask;
            });
        services.AddScoped<AnaliseRiscoGrpc>();
        services.AddScoped<IAnaliseRisco>(provedor => provedor.GetRequiredService<IOptions<OpcoesAntifraude>>().Value.Ativo
            ? provedor.GetRequiredService<AnaliseRiscoGrpc>()
            : new AnaliseRiscoDesligada());
    }
}
