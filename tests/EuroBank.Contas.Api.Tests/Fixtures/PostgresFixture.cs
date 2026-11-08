using EuroBank.Contas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(EuroBank.Contas.Api.Tests.Fixtures.PostgresFixture))]

namespace EuroBank.Contas.Api.Tests.Fixtures;

/// <summary>
/// Um Postgres real (container) compartilhado pelo assembly. O database padrão recebe as migrations para os
/// testes de persistência; a API de testes usa um database próprio, migrado e semeado pela própria API.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18.6")
        .WithDatabase("eurobank_contas")
        .WithUsername("eurobank")
        .WithPassword("eurobank")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    /// <summary>API de Contas compartilhada pelos testes de endpoints.</summary>
    public ContasApiFactory Api { get; private set; } = null!;

    public string ConnectionStringApi { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        await using (var contexto = CriarContexto())
        {
            await contexto.Database.MigrateAsync();
        }

        ConnectionStringApi = await CriarBancoVazioAsync();
        Api = new ContasApiFactory(ConnectionStringApi);

        // Sobe a API já aqui: a WebApplicationFactory não é segura para a primeira subida em paralelo, e várias
        // classes de teste a usam ao mesmo tempo (duas subidas rodariam as migrations simultaneamente).
        using var _ = Api.CreateClient();
    }

    public ContasDbContext CriarContexto(string? connectionString = null) =>
        new(new DbContextOptionsBuilder<ContasDbContext>()
            .UseNpgsql(connectionString ?? ConnectionString)
            .Options);

    /// <summary>Cria um database vazio no mesmo container e devolve a connection string dele.</summary>
    public async Task<string> CriarBancoVazioAsync()
    {
        var nome = $"teste_{Guid.NewGuid():N}";
        await using (var conexao = new NpgsqlConnection(ConnectionString))
        {
            await conexao.OpenAsync();
            await using var comando = new NpgsqlCommand($"CREATE DATABASE {nome}", conexao);
            await comando.ExecuteNonQueryAsync();
        }

        return new NpgsqlConnectionStringBuilder(ConnectionString) { Database = nome }.ConnectionString;
    }

    public async ValueTask DisposeAsync()
    {
        await Api.DisposeAsync();
        await _container.DisposeAsync();
    }
}
