using EuroBank.Extrato.Infrastructure.Enums;
using EuroBank.Extrato.Worker;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;

namespace EuroBank.Extrato.Worker.Tests.Fixtures;

/// <summary>
/// Um worker do Extrato in-process com a estratégia de cache escolhida, o transporte Kafka desligado e os serviços
/// periódicos parados (os testes chamam a descarga e a reconstrução direto).
/// </summary>
public sealed class WorkerExtrato : IAsyncDisposable
{
    private readonly WebApplicationFactory<MarcadorExtratoWorker> _fabrica;

    public WorkerExtrato(string postgres, string redis, EstrategiaCache estrategia)
    {
        _fabrica = new WebApplicationFactory<MarcadorExtratoWorker>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:Extrato", postgres);
            builder.UseSetting("ConnectionStrings:Redis", redis);
            builder.UseSetting("Cache:Estrategia", estrategia.ToString());
            builder.UseSetting("Cache:IntervaloDescarga", "01:00:00");
            builder.UseSetting("Cache:IntervaloReconstrucao", "01:00:00");
            // Sem confiar na leitura anterior da marca: cada teste enxerga na hora a marca que outro gravou.
            builder.UseSetting("Cache:ValidadeLeituraMarca", "00:00:00");
            builder.UseSetting("Seguranca:ApiKey", ExtratoWorkerFixture.ApiKey);
            builder.UseSetting("OTEL_SDK_DISABLED", "true");
            builder.ConfigureServices(services => services.DisableAllExternalWolverineTransports());
        });

        Http = _fabrica.CreateClient();
        Http.DefaultRequestHeaders.Add("X-Api-Key", ExtratoWorkerFixture.ApiKey);
    }

    public HttpClient Http { get; }

    public IServiceProvider Servicos => _fabrica.Services;

    public HttpClient CriarClienteSemChave() => _fabrica.CreateClient();

    /// <summary>Entrega um evento ao manipulador, como o listener do Kafka faria.</summary>
    public async Task EntregarAsync(object evento)
    {
        await using var escopo = _fabrica.Services.CreateAsyncScope();
        await escopo.ServiceProvider.GetRequiredService<IMessageBus>().InvokeAsync(evento, TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        Http.Dispose();
        await _fabrica.DisposeAsync();
    }
}
