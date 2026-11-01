using EuroBank.Contas.Infrastructure.Outbox;
using EuroBank.Contas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(EuroBank.Contas.Api.Tests.Fixtures.PostgresFixture))]

namespace EuroBank.Contas.Api.Tests.Fixtures;

/// <summary>Um Postgres real (container) compartilhado por todos os testes do assembly, já com as migrations.</summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18.6")
        .WithDatabase("eurobank_contas")
        .WithUsername("eurobank")
        .WithPassword("eurobank")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        await using var contexto = CriarContexto();
        await contexto.Database.MigrateAsync();
    }

    public ContasDbContext CriarContexto() =>
        new(new DbContextOptionsBuilder<ContasDbContext>()
            .UseNpgsql(ConnectionString)
            .AddInterceptors(new InterceptorOutbox(new MapeadorMensagensOutbox()))
            .Options);

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}
