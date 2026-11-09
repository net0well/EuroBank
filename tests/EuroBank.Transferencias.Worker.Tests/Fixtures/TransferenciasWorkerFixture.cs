using EuroBank.Shared.Domain;
using EuroBank.Shared.ValueObjects;
using EuroBank.Transferencias.Domain.Entities;
using EuroBank.Transferencias.Domain.Repositories;
using EuroBank.Transferencias.Domain.ValueObjects;
using EuroBank.Transferencias.Infrastructure.Persistence;
using EuroBank.Transferencias.Worker;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Testcontainers.PostgreSql;
using Wolverine;

[assembly: AssemblyFixture(typeof(EuroBank.Transferencias.Worker.Tests.Fixtures.TransferenciasWorkerFixture))]

namespace EuroBank.Transferencias.Worker.Tests.Fixtures;

/// <summary>
/// Worker de Transferências in-process contra um Postgres real, com o Kafka desligado e um relógio falso: os
/// testes entregam as respostas pelo barramento e chamam a varredura direto, avançando o tempo à mão.
/// </summary>
public sealed class TransferenciasWorkerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18.6")
        .WithDatabase("eurobank_transferencias")
        .WithUsername("eurobank")
        .WithPassword("eurobank")
        .Build();

    private WebApplicationFactory<MarcadorTransferenciasWorker> _worker = null!;

    public FakeTimeProvider Relogio { get; } = new(new DateTimeOffset(2026, 11, 9, 10, 0, 0, TimeSpan.Zero));

    public string ConnectionString => _container.GetConnectionString();

    public IServiceProvider Servicos => _worker.Services;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        // No compose, quem aplica as migrations é a API; aqui o teste aplica antes de subir o worker.
        await using (var contexto = CriarContexto())
        {
            await contexto.Database.MigrateAsync();
        }

        _worker = new WebApplicationFactory<MarcadorTransferenciasWorker>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Transferencias", ConnectionString);
            builder.UseSetting("OTEL_SDK_DISABLED", "true");
            // A varredura automática não dispara durante os testes: eles chamam a varredura direto.
            builder.UseSetting("Varredura:Intervalo", "01:00:00");
            builder.ConfigureServices(services =>
            {
                services.DisableAllExternalWolverineTransports();
                services.AddSingleton<TimeProvider>(Relogio);
            });
        });
        using var _ = _worker.CreateClient();
    }

    public TransferenciasDbContext CriarContexto() =>
        new(new DbContextOptionsBuilder<TransferenciasDbContext>().UseNpgsql(ConnectionString).Options);

    /// <summary>Entrega uma resposta ao manipulador, como o listener do Kafka faria.</summary>
    public async Task EntregarAsync(object resposta)
    {
        await using var escopo = _worker.Services.CreateAsyncScope();
        await escopo.ServiceProvider.GetRequiredService<IMessageBus>()
            .InvokeAsync(resposta, TestContext.Current.CancellationToken);
    }

    /// <summary>Cria uma transferência externa e, se for para já, começa a saga (débito pedido).</summary>
    public async Task<TransferenciaId> CriarAsync(string iban = "DE89370400440532013000", DateTimeOffset? executarEm = null)
    {
        await using var escopo = _worker.Services.CreateAsyncScope();
        var servicos = escopo.ServiceProvider;
        var agora = Relogio.GetUtcNow();
        var transferencia = Transferencia.SolicitarExterna(
            Guid.CreateVersion7(),
            IbanDestino.Criar(iban),
            "DEUTDEFF",
            "Bruno",
            Dinheiro.De(100m),
            null,
            null,
            Guid.CreateVersion7().ToString(),
            "hash",
            executarEm,
            agora);
        transferencia.Iniciar(Dinheiro.Zero, servicos.GetRequiredService<PoliticaSaga>(), agora);
        servicos.GetRequiredService<IRepositorioTransferencias>().Adicionar(transferencia);
        await servicos.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(TestContext.Current.CancellationToken);
        return transferencia.Id;
    }

    public async Task<Transferencia> ObterAsync(TransferenciaId id)
    {
        await using var contexto = CriarContexto();
        return await contexto.Transferencias.AsNoTracking().SingleAsync(t => t.Id == id, TestContext.Current.CancellationToken);
    }

    /// <summary>Tipos das mensagens que a saga gravou na outbox para esta transferência, na ordem.</summary>
    public async Task<List<string>> ComandosAsync(TransferenciaId id, Guid? contaOrigemId = null)
    {
        await using var contexto = CriarContexto();
        var chaves = new[] { id.ToString(), contaOrigemId?.ToString() ?? id.ToString() };
        return await contexto.Outbox
            .Where(mensagem => chaves.Contains(mensagem.IdAgregado))
            .OrderBy(mensagem => mensagem.Sequencia)
            .Select(mensagem => mensagem.TipoEvento)
            .ToListAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _worker.DisposeAsync();
        await _container.DisposeAsync();
    }
}
