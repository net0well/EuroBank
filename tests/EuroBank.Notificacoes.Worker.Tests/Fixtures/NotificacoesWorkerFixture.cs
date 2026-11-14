using EuroBank.Notificacoes.Worker.Entities;
using EuroBank.Notificacoes.Worker.Persistence;
using EuroBank.Notificacoes.Worker.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Wolverine;

[assembly: AssemblyFixture(typeof(EuroBank.Notificacoes.Worker.Tests.Fixtures.NotificacoesWorkerFixture))]

namespace EuroBank.Notificacoes.Worker.Tests.Fixtures;

/// <summary>
/// Sobe o worker de notificações in-process contra um Postgres real, com o Kafka desligado e um enviador de e-mail
/// falso; as esperas entre tentativas ficam curtas para o retry caber no teste.
/// </summary>
public sealed class NotificacoesWorkerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18.6")
        .WithDatabase("eurobank_notificacoes")
        .WithUsername("eurobank")
        .WithPassword("eurobank")
        .Build();

    private WebApplicationFactory<MarcadorNotificacoesWorker> _worker = null!;

    public EnviadorEmailFalso Enviador { get; } = new();

    public HttpClient Http { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        _worker = new WebApplicationFactory<MarcadorNotificacoesWorker>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:Notificacoes", _container.GetConnectionString());
            builder.UseSetting("OTEL_SDK_DISABLED", "true");
            builder.UseSetting("Notificacoes:EsperasEntreTentativas:0", "00:00:00.100");
            builder.UseSetting("Notificacoes:EsperasEntreTentativas:1", "00:00:00.200");
            builder.UseSetting("Notificacoes:EsperasEntreTentativas:2", "00:00:00.300");
            builder.ConfigureServices(services =>
            {
                services.DisableAllExternalWolverineTransports();
                services.RemoveAll<IEnviadorEmail>();
                services.AddSingleton<IEnviadorEmail>(Enviador);
            });
        });

        Http = _worker.CreateClient();
    }

    /// <summary>Entrega o evento ao manipulador, como o listener do Kafka faria (falhas sobem para o teste).</summary>
    public async Task EntregarAsync(object evento)
    {
        await using var escopo = _worker.Services.CreateAsyncScope();
        await escopo.ServiceProvider.GetRequiredService<IMessageBus>()
            .InvokeAsync(evento, TestContext.Current.CancellationToken);
    }

    /// <summary>Publica o evento numa fila local: passa pela política de erros (retry agendado e dead letter).</summary>
    public async Task PublicarAsync(object evento)
    {
        await using var escopo = _worker.Services.CreateAsyncScope();
        await escopo.ServiceProvider.GetRequiredService<IMessageBus>().PublishAsync(evento);
    }

    /// <summary>Executa algo com o serviço de notificações num escopo próprio (como um manipulador faria).</summary>
    public async Task ComServicoAsync(Func<ServicoNotificacoes, Task> acao)
    {
        await using var escopo = _worker.Services.CreateAsyncScope();
        await acao(escopo.ServiceProvider.GetRequiredService<ServicoNotificacoes>());
    }

    public async Task<List<Notificacao>> NotificacoesDaTransferenciaAsync(Guid transferenciaId)
    {
        await using var contexto = CriarContexto();
        return await contexto.Notificacoes.AsNoTracking()
            .Where(notificacao => notificacao.TransferenciaId == transferenciaId)
            .ToListAsync(TestContext.Current.CancellationToken);
    }

    public NotificacoesDbContext CriarContexto() =>
        new(new DbContextOptionsBuilder<NotificacoesDbContext>().UseNpgsql(_container.GetConnectionString()).Options);

    public async ValueTask DisposeAsync()
    {
        Http.Dispose();
        await _worker.DisposeAsync();
        await _container.DisposeAsync();
    }
}
