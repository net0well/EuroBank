using EuroBank.Antifraude.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace EuroBank.Antifraude.Treinamento.Tests.Fixtures;

/// <summary>Um Postgres real com as migrations do Antifraude: de onde o treino lê os rótulos e onde registra a versão.</summary>
public sealed class BancoAntifraudeFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18.6")
        .WithDatabase("eurobank_antifraude")
        .WithUsername("eurobank")
        .WithPassword("eurobank")
        .Build();

    public AntifraudeDbContext CriarContexto() =>
        new(new DbContextOptionsBuilder<AntifraudeDbContext>().UseNpgsql(_postgres.GetConnectionString()).Options);

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
        await using var contexto = CriarContexto();
        await contexto.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync() => await _postgres.DisposeAsync();
}
