using EuroBank.Extrato.Worker;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Wolverine;

[assembly: AssemblyFixture(typeof(EuroBank.Extrato.Worker.Tests.Fixtures.ExtratoWorkerFixture))]

namespace EuroBank.Extrato.Worker.Tests.Fixtures;

/// <summary>
/// Sobe o worker do Extrato in-process contra um Postgres real, com o transporte Kafka desligado: os testes
/// entregam as mensagens direto aos manipuladores pelo IMessageBus.
/// </summary>
public sealed class ExtratoWorkerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18.6")
        .WithDatabase("eurobank_extrato")
        .WithUsername("eurobank")
        .WithPassword("eurobank")
        .Build();

    private WebApplicationFactory<MarcadorExtratoWorker> _worker = null!;

    public HttpClient Http { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        _worker = new WebApplicationFactory<MarcadorExtratoWorker>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:Extrato", _container.GetConnectionString());
            builder.UseSetting("OTEL_SDK_DISABLED", "true");
            builder.ConfigureServices(services => services.DisableAllExternalWolverineTransports());
        });

        Http = _worker.CreateClient();
    }

    /// <summary>Entrega um evento ao manipulador, como o listener do Kafka faria.</summary>
    public async Task EntregarAsync(object evento)
    {
        await using var escopo = _worker.Services.CreateAsyncScope();
        await escopo.ServiceProvider.GetRequiredService<IMessageBus>()
            .InvokeAsync(evento, TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        Http.Dispose();
        await _worker.DisposeAsync();
        await _container.DisposeAsync();
    }
}
