using System.Globalization;
using EuroBank.Shared.Enums;
using EuroBank.Transferencias.Infrastructure.Persistence;
using EuroBank.Transferencias.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

[assembly: AssemblyFixture(typeof(EuroBank.Transferencias.Api.Tests.Fixtures.PostgresFixture))]

namespace EuroBank.Transferencias.Api.Tests.Fixtures;

/// <summary>Postgres e Redis reais (containers) e a API de Transferências in-process, compartilhados pelo assembly.</summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    /// <summary>Conta EUR já na projeção, para os testes que não se importam com a conta de origem.</summary>
    public static readonly Guid ContaPadrao = Guid.CreateVersion7();

    private static long _sequencialIban = Random.Shared.NextInt64(1, 1_000_000_000);

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18.6")
        .WithDatabase("eurobank_transferencias")
        .WithUsername("eurobank")
        .WithPassword("eurobank")
        .Build();

    /// <summary>Um Redis que não existe: cada operação falha na hora, como com o Redis fora do ar.</summary>
    public const string RedisForaDoAr = "localhost:1,abortConnect=false,connectTimeout=300";

    private readonly RedisContainer _redis = new RedisBuilder("redis:8.10.3").Build();

    public TransferenciasApiFactory Api { get; private set; } = null!;

    public string ConnectionString => _container.GetConnectionString();

    /// <summary>Conexão própria dos testes, para ver e mexer nas keys do Redis.</summary>
    public IConnectionMultiplexer Redis { get; private set; } = null!;

    /// <summary>Outra API, no mesmo banco, com o Redis pedido; quem chama descarta.</summary>
    public TransferenciasApiFactory CriarApi(string redis) => new(ConnectionString, redis);

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_container.StartAsync(), _redis.StartAsync());
        Redis = await ConnectionMultiplexer.ConnectAsync(_redis.GetConnectionString());
        Api = new TransferenciasApiFactory(_container.GetConnectionString(), _redis.GetConnectionString());

        // Sobe a API já aqui (aplica as migrations uma vez só): a primeira subida da WebApplicationFactory não é
        // segura em paralelo.
        using var _ = Api.CreateClient();
        await RegistrarContaAsync(ContaPadrao, Moeda.EUR);
    }

    public TransferenciasDbContext CriarContexto() =>
        new(new DbContextOptionsBuilder<TransferenciasDbContext>().UseNpgsql(_container.GetConnectionString()).Options);

    /// <summary>Uma conta EuroBank nova na projeção local, como se o ContaAberta já tivesse chegado.</summary>
    public async Task<ContaConhecida> NovaContaAsync(Moeda moeda = Moeda.EUR) =>
        await RegistrarContaAsync(Guid.CreateVersion7(), moeda);

    private async Task<ContaConhecida> RegistrarContaAsync(Guid id, Moeda moeda)
    {
        var sequencial = Interlocked.Increment(ref _sequencialIban).ToString("D13", CultureInfo.InvariantCulture);
        var conta = new ContaConhecida
        {
            Id = id,
            Iban = $"PT50EURO{sequencial}",
            Titular = $"Titular {moeda}",
            Moeda = moeda,
            AbertaEm = DateTimeOffset.UtcNow,
        };

        await using var contexto = CriarContexto();
        contexto.ContasConhecidas.Add(conta);
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        return conta;
    }

    public async ValueTask DisposeAsync()
    {
        await Api.DisposeAsync();
        Redis.Dispose();
        await Task.WhenAll(_container.DisposeAsync().AsTask(), _redis.DisposeAsync().AsTask());
    }
}
