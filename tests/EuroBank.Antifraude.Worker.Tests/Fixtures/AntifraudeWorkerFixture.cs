using EuroBank.Antifraude.Application.Interfaces;
using EuroBank.Antifraude.Domain.ValueObjects;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Wolverine;

[assembly: AssemblyFixture(typeof(EuroBank.Antifraude.Worker.Tests.Fixtures.AntifraudeWorkerFixture))]

namespace EuroBank.Antifraude.Worker.Tests.Fixtures;

/// <summary>
/// Worker do Antifraude in-process contra Postgres e Redis reais, com o Kafka desligado: os testes entregam os
/// eventos de Contas direto aos manipuladores e leem o perfil que ficou no Redis.
/// </summary>
public sealed class AntifraudeWorkerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18.6")
        .WithDatabase("eurobank_antifraude")
        .WithUsername("eurobank")
        .WithPassword("eurobank")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder("redis:8.10.3").Build();

    private WebApplicationFactory<MarcadorAntifraudeWorker> _worker = null!;

    public string ConnectionStringRedis => $"{_redis.GetConnectionString()},allowAdmin=true";

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync());

        _worker = new WebApplicationFactory<MarcadorAntifraudeWorker>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:Antifraude", _postgres.GetConnectionString());
            builder.UseSetting("ConnectionStrings:Redis", ConnectionStringRedis);
            builder.UseSetting("OTEL_SDK_DISABLED", "true");
            builder.ConfigureServices(services => services.DisableAllExternalWolverineTransports());
        });
        using var _ = _worker.CreateClient();
    }

    public IServiceProvider Servicos => _worker.Services;

    /// <summary>Entrega um evento ao manipulador, como o listener do Kafka faria.</summary>
    public async Task EntregarAsync(object evento)
    {
        await using var escopo = _worker.Services.CreateAsyncScope();
        await escopo.ServiceProvider.GetRequiredService<IMessageBus>().InvokeAsync(evento, TestContext.Current.CancellationToken);
    }

    public async Task<PerfilConta> PerfilAsync(Guid contaId, DateTimeOffset agora)
    {
        await using var escopo = _worker.Services.CreateAsyncScope();
        return await escopo.ServiceProvider.GetRequiredService<IPerfisContas>().ObterAsync(contaId, agora, TestContext.Current.CancellationToken);
    }

    /// <summary>Simula o Redis perdendo os dados.</summary>
    public async Task ApagarRedisAsync()
    {
        var redis = _worker.Services.GetRequiredService<IConnectionMultiplexer>();
        foreach (var servidor in redis.GetServers())
        {
            await servidor.FlushAllDatabasesAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _worker.DisposeAsync();
        await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _redis.DisposeAsync().AsTask());
    }
}
