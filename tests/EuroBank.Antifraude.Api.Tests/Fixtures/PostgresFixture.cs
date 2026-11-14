using EuroBank.Antifraude.Infrastructure;
using EuroBank.Antifraude.Infrastructure.Persistence;
using EuroBank.Antifraude.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(EuroBank.Antifraude.Api.Tests.Fixtures.PostgresFixture))]

namespace EuroBank.Antifraude.Api.Tests.Fixtures;

/// <summary>Postgres real com as migrations e os dados iniciais do Antifraude, e a infraestrutura registrada.</summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18.6")
        .WithDatabase("eurobank_antifraude")
        .WithUsername("eurobank")
        .WithPassword("eurobank")
        .Build();

    public ServiceProvider Servicos { get; private set; } = null!;

    public string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Antifraude"] = ConnectionString })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuracao);
        services.AddSingleton(TimeProvider.System);
        services.AdicionarInfraestruturaAntifraude();
        Servicos = services.BuildServiceProvider();

        await using var escopo = Servicos.CreateAsyncScope();
        await escopo.ServiceProvider.GetRequiredService<AntifraudeDbContext>().Database.MigrateAsync();
        await escopo.ServiceProvider.GetRequiredService<SemeadorAntifraude>().SemearAsync(TestContext.Current.CancellationToken);
    }

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
        await Servicos.DisposeAsync();
        await _container.DisposeAsync();
    }
}
