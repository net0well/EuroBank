using EuroBank.Antifraude.Api;
using EuroBank.Antifraude.Application.Interfaces;
using EuroBank.Antifraude.Grpc;
using EuroBank.Antifraude.Infrastructure;
using EuroBank.Antifraude.Infrastructure.Persistence;
using EuroBank.Antifraude.Infrastructure.Persistence.Seed;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

[assembly: AssemblyFixture(typeof(EuroBank.Antifraude.Api.Tests.Fixtures.AntifraudeFixture))]

namespace EuroBank.Antifraude.Api.Tests.Fixtures;

/// <summary>
/// Postgres e Redis reais, a infraestrutura registrada (para os testes de persistência) e a API do Antifraude
/// in-process (gRPC sobre o TestServer, JSON transcoding e REST).
/// </summary>
public sealed class AntifraudeFixture : IAsyncLifetime
{
    public const string ApiKey = "chave-de-teste-antifraude";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18.6")
        .WithDatabase("eurobank_antifraude")
        .WithUsername("eurobank")
        .WithPassword("eurobank")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder("redis:8.10.3").Build();

    public ServiceProvider Servicos { get; private set; } = null!;

    public WebApplicationFactory<MarcadorAntifraudeApi> Api { get; private set; } = null!;

    public string ConnectionString => _postgres.GetConnectionString();

    public string ConnectionStringRedis => _redis.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync());

        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Antifraude"] = ConnectionString })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuracao);
        services.AddSingleton(TimeProvider.System);
        services.AdicionarInfraestruturaAntifraude();
        Servicos = services.BuildServiceProvider();

        await using (var escopo = Servicos.CreateAsyncScope())
        {
            await escopo.ServiceProvider.GetRequiredService<AntifraudeDbContext>().Database.MigrateAsync();
            await escopo.ServiceProvider.GetRequiredService<SemeadorAntifraude>().SemearAsync(TestContext.Current.CancellationToken);
        }

        Api = CriarApi(ConnectionStringRedis);
        using var _ = Api.CreateClient();
    }

    /// <summary>Uma API in-process apontando para o Redis informado (os testes de Redis fora do ar passam outro).</summary>
    public WebApplicationFactory<MarcadorAntifraudeApi> CriarApi(string redis) =>
        new WebApplicationFactory<MarcadorAntifraudeApi>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:Antifraude", ConnectionString);
            builder.UseSetting("ConnectionStrings:Redis", redis);
            builder.UseSetting("Seguranca:ApiKey", ApiKey);
            builder.UseSetting("OTEL_SDK_DISABLED", "true");
        });

    /// <summary>Cliente gRPC da API in-process, mandando a API key na metadata (nula: sem key).</summary>
    public AntifraudeService.AntifraudeServiceClient CriarClienteGrpc(
        string? apiKey = ApiKey,
        WebApplicationFactory<MarcadorAntifraudeApi>? api = null)
    {
        var fabrica = api ?? Api;
        var canal = GrpcChannel.ForAddress(fabrica.Server.BaseAddress, new GrpcChannelOptions { HttpHandler = fabrica.Server.CreateHandler() });
        var invocador = canal.Intercept(metadata =>
        {
            if (apiKey is not null)
            {
                metadata.Add("x-api-key", apiKey);
            }

            return metadata;
        });
        return new AntifraudeService.AntifraudeServiceClient(invocador);
    }

    public GrpcChannel CriarCanal() =>
        GrpcChannel.ForAddress(Api.Server.BaseAddress, new GrpcChannelOptions { HttpHandler = Api.Server.CreateHandler() });

    /// <summary>Cliente HTTP (REST e JSON transcoding) com a API key.</summary>
    public HttpClient CriarClienteHttp(bool comApiKey = true)
    {
        var cliente = Api.CreateClient();
        if (comApiKey)
        {
            cliente.DefaultRequestHeaders.Add("X-Api-Key", ApiKey);
        }

        return cliente;
    }

    /// <summary>O perfil de comportamento que a API lê no Redis (os testes o montam direto).</summary>
    public IPerfisContas Perfis => Api.Services.GetRequiredService<IPerfisContas>();

    /// <summary>Executa algo num escopo próprio, como uma requisição ou uma mensagem faria.</summary>
    public async Task EmEscopoAsync(Func<IServiceProvider, Task> acao)
    {
        await using var escopo = Servicos.CreateAsyncScope();
        await acao(escopo.ServiceProvider);
    }

    public AntifraudeDbContext CriarContexto() =>
        new(new DbContextOptionsBuilder<AntifraudeDbContext>().UseNpgsql(ConnectionString).Options);

    public async ValueTask DisposeAsync()
    {
        await Api.DisposeAsync();
        await Servicos.DisposeAsync();
        await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _redis.DisposeAsync().AsTask());
    }
}
