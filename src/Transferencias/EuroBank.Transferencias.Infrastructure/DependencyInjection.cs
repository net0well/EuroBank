using EuroBank.Antifraude.Grpc;
using EuroBank.Shared.Domain;
using EuroBank.Shared.Persistence.Outbox;
using EuroBank.Shared.Persistence.Transactions;
using EuroBank.Transferencias.Application.Interfaces;
using EuroBank.Transferencias.Domain.Repositories;
using EuroBank.Transferencias.Infrastructure.Clients;
using EuroBank.Transferencias.Infrastructure.Idempotency;
using EuroBank.Transferencias.Infrastructure.Options;
using EuroBank.Transferencias.Infrastructure.Outbox;
using EuroBank.Transferencias.Infrastructure.Persistence;
using EuroBank.Transferencias.Infrastructure.Persistence.Queries;
using EuroBank.Transferencias.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

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
        services.AdicionarClienteAntifraude();
        services.AdicionarRedis();
        services.AddScoped<IReservasIdempotencia, ReservasIdempotenciaRedis>();
        return services;
    }

    /// <summary>
    /// Uma conexão com o Redis (Idempotency-Key). Com abortConnect=false a aplicação sobe com o Redis fora; com o
    /// backlog em fail-fast, cada operação falha na hora em vez de esperar o timeout, e o pedido segue pelo Postgres.
    /// </summary>
    private static void AdicionarRedis(this IServiceCollection services) =>
        services.TryAddSingleton<IConnectionMultiplexer>(provedor =>
        {
            var configuracao = provedor.GetRequiredService<IConfiguration>().GetConnectionString("Redis")
                ?? throw new InvalidOperationException("ConnectionStrings:Redis não configurada.");
            var opcoes = ConfigurationOptions.Parse(configuracao);
            opcoes.AbortOnConnectFail = false;
            opcoes.BacklogPolicy = BacklogPolicy.FailFast;
            return ConnectionMultiplexer.Connect(opcoes);
        });

    /// <summary>
    /// Cliente gRPC do antifraude, registrado também na API: a varredura (e o que depende dela) é validada quando o
    /// container é montado. Só o worker chama de fato.
    /// </summary>
    private static void AdicionarClienteAntifraude(this IServiceCollection services)
    {
        services.AddOptions<OpcoesAntifraude>().BindConfiguration(OpcoesAntifraude.Secao);
        services
            .AddGrpcClient<AntifraudeService.AntifraudeServiceClient>(OpcoesAntifraude.NomeCliente, (provedor, cliente) =>
                cliente.Address = provedor.GetRequiredService<IOptions<OpcoesAntifraude>>().Value.Endereco)
            .ConfigureChannel((provedor, canal) =>
            {
                // No laboratório o canal é h2c (sem TLS): sem esta opção, a API key não iria na chamada.
                canal.UnsafeUseInsecureChannelCallCredentials = true;
                canal.ServiceConfig = ConfiguracaoCanalAntifraude.Criar(provedor.GetRequiredService<IOptions<OpcoesAntifraude>>().Value);
            })
            .AddCallCredentials((_, metadata, provedor) =>
            {
                metadata.Add("x-api-key", provedor.GetRequiredService<IOptions<OpcoesAntifraude>>().Value.ApiKey);
                return Task.CompletedTask;
            });
        services.AddScoped<IAvaliacaoRisco, AvaliacaoRiscoGrpc>();
    }
}
