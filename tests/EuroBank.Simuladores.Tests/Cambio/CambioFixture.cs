using EuroBank.Simuladores.Cambio.Persistencia;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace EuroBank.Simuladores.Tests.Cambio;

/// <summary>Postgres e Redis reais para o simulador de câmbio, mais uma conexão com um Redis que não existe.</summary>
public sealed class CambioFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18.6")
        .WithDatabase("eurobank_cambio")
        .WithUsername("eurobank")
        .WithPassword("eurobank")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder("redis:8.10.3").Build();

    public RepositorioCambio Repositorio { get; private set; } = null!;

    public IConnectionMultiplexer Redis { get; private set; } = null!;

    /// <summary>Redis fora do ar: com fail-fast, cada operação falha na hora.</summary>
    public IConnectionMultiplexer RedisForaDoAr { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_container.StartAsync(), _redis.StartAsync());
        Repositorio = new RepositorioCambio(_container.GetConnectionString());
        await Repositorio.CriarEstruturaAsync(TestContext.Current.CancellationToken);
        Redis = await ConnectionMultiplexer.ConnectAsync(_redis.GetConnectionString());

        var foraDoAr = ConfigurationOptions.Parse("localhost:1,abortConnect=false,connectTimeout=300");
        foraDoAr.BacklogPolicy = BacklogPolicy.FailFast;
        RedisForaDoAr = await ConnectionMultiplexer.ConnectAsync(foraDoAr);
    }

    public async ValueTask DisposeAsync()
    {
        Redis.Dispose();
        RedisForaDoAr.Dispose();
        await Task.WhenAll(_container.DisposeAsync().AsTask(), _redis.DisposeAsync().AsTask());
    }
}
