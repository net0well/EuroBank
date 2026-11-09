using EuroBank.Transferencias.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(EuroBank.Transferencias.Api.Tests.Fixtures.PostgresFixture))]

namespace EuroBank.Transferencias.Api.Tests.Fixtures;

/// <summary>Um Postgres real (container) e a API de Transferências in-process, compartilhados pelo assembly.</summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18.6")
        .WithDatabase("eurobank_transferencias")
        .WithUsername("eurobank")
        .WithPassword("eurobank")
        .Build();

    public TransferenciasApiFactory Api { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        Api = new TransferenciasApiFactory(_container.GetConnectionString());

        // Sobe a API já aqui (aplica as migrations uma vez só): a primeira subida da WebApplicationFactory não é
        // segura em paralelo.
        using var _ = Api.CreateClient();
    }

    public TransferenciasDbContext CriarContexto() =>
        new(new DbContextOptionsBuilder<TransferenciasDbContext>().UseNpgsql(_container.GetConnectionString()).Options);

    public async ValueTask DisposeAsync()
    {
        await Api.DisposeAsync();
        await _container.DisposeAsync();
    }
}
