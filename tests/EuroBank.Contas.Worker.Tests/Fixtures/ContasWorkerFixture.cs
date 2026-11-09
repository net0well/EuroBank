using EuroBank.Contas.Infrastructure.Persistence;
using EuroBank.Contas.Worker;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Wolverine;

[assembly: AssemblyFixture(typeof(EuroBank.Contas.Worker.Tests.Fixtures.ContasWorkerFixture))]

namespace EuroBank.Contas.Worker.Tests.Fixtures;

/// <summary>Worker de Contas in-process contra um Postgres real, com o transporte Kafka desligado.</summary>
public sealed class ContasWorkerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18.6")
        .WithDatabase("eurobank_contas")
        .WithUsername("eurobank")
        .WithPassword("eurobank")
        .Build();

    private WebApplicationFactory<MarcadorContasWorker> _worker = null!;

    public string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        // No compose, quem aplica as migrations é a API de Contas; aqui o teste aplica antes de subir o worker.
        await using (var contexto = CriarContexto())
        {
            await contexto.Database.MigrateAsync();
        }

        _worker = new WebApplicationFactory<MarcadorContasWorker>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Contas", ConnectionString);
            builder.UseSetting("OTEL_SDK_DISABLED", "true");
            builder.ConfigureServices(services => services.DisableAllExternalWolverineTransports());
        });
        using var _ = _worker.CreateClient();
    }

    public ContasDbContext CriarContexto() =>
        new(new DbContextOptionsBuilder<ContasDbContext>().UseNpgsql(ConnectionString).Options);

    /// <summary>Entrega um comando ao manipulador, como o listener do Kafka faria.</summary>
    public async Task EntregarAsync(object comando)
    {
        await using var escopo = _worker.Services.CreateAsyncScope();
        await escopo.ServiceProvider.GetRequiredService<IMessageBus>()
            .InvokeAsync(comando, TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _worker.DisposeAsync();
        await _container.DisposeAsync();
    }
}
