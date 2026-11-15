using EuroBank.Extrato.Infrastructure.Enums;
using EuroBank.Extrato.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

[assembly: AssemblyFixture(typeof(EuroBank.Extrato.Worker.Tests.Fixtures.ExtratoWorkerFixture))]

namespace EuroBank.Extrato.Worker.Tests.Fixtures;

/// <summary>
/// Postgres e Redis reais e o worker do Extrato in-process com o write-through (o padrão), com o transporte Kafka
/// desligado: os testes entregam as mensagens direto aos manipuladores pelo IMessageBus. Os testes do cache sobem
/// outros workers com as demais estratégias, no mesmo Postgres e no mesmo Redis.
/// </summary>
public sealed class ExtratoWorkerFixture : IAsyncLifetime
{
    public const string ApiKey = "chave-de-teste-extrato";

    /// <summary>Um Redis que não existe, com prazos curtos: as operações falham logo, como com o Redis fora do ar.</summary>
    public const string RedisForaDoAr = "localhost:1,abortConnect=false,connectTimeout=300,asyncTimeout=300,syncTimeout=300";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18.6")
        .WithDatabase("eurobank_extrato")
        .WithUsername("eurobank")
        .WithPassword("eurobank")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder("redis:8.10.3").Build();

    private WorkerExtrato _worker = null!;

    public HttpClient Http => _worker.Http;

    public IServiceProvider Servicos => _worker.Servicos;

    public string ConnectionStringPostgres => _postgres.GetConnectionString();

    public string ConnectionStringRedis => $"{_redis.GetConnectionString()},allowAdmin=true";

    /// <summary>Conexão própria dos testes, para conferir o que ficou no Redis.</summary>
    public IConnectionMultiplexer Redis { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync());
        Redis = await ConnectionMultiplexer.ConnectAsync(ConnectionStringRedis);
        _worker = CriarWorker(EstrategiaCache.WriteThrough);
    }

    /// <summary>Outro worker, com a estratégia e o Redis pedidos; quem chama descarta.</summary>
    public WorkerExtrato CriarWorker(EstrategiaCache estrategia, string? redis = null) =>
        new(ConnectionStringPostgres, redis ?? ConnectionStringRedis, estrategia);

    public ExtratoDbContext CriarContexto() =>
        new(new DbContextOptionsBuilder<ExtratoDbContext>().UseNpgsql(ConnectionStringPostgres).Options);

    /// <summary>Cliente HTTP sem nenhuma API key (para testar o 401).</summary>
    public HttpClient CriarClienteSemChave() => _worker.CriarClienteSemChave();

    /// <summary>Entrega um evento ao manipulador, como o listener do Kafka faria.</summary>
    public Task EntregarAsync(object evento) => _worker.EntregarAsync(evento);

    public async ValueTask DisposeAsync()
    {
        await _worker.DisposeAsync();
        Redis.Dispose();
        await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _redis.DisposeAsync().AsTask());
    }
}
