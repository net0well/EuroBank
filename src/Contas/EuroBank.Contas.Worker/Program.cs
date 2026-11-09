using EuroBank.Contas.Application;
using EuroBank.Contas.Application.Handlers;
using EuroBank.Contas.Infrastructure;
using EuroBank.Contas.Infrastructure.Persistence;
using EuroBank.Contas.Worker.Messaging;
using EuroBank.Shared.Messaging;
using EuroBank.Shared.Web.Extensions;
using JasperFx.Resources;
using Wolverine;

// Worker de Contas: executa os comandos das sagas de Transferencias (debitar, estornar, liberar trava) e responde
// pela outbox. Separado da API para escalar e cair de forma independente dela.
var builder = WebApplication.CreateBuilder(args);

builder.AdicionarObservabilidade("eurobank-contas-worker");

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AdicionarAplicacaoContas();
builder.Services.AdicionarInfraestruturaContas();

builder.Host.UseWolverine(opcoes =>
{
    var kafka = builder.Configuration.GetSection(OpcoesKafka.Secao).Get<OpcoesKafka>() ?? new OpcoesKafka();
    if (string.IsNullOrWhiteSpace(kafka.GrupoConsumidor))
    {
        kafka.GrupoConsumidor = "eurobank-contas-worker";
    }

    var connectionString = builder.Configuration.GetConnectionString("Contas")
        ?? throw new InvalidOperationException("ConnectionStrings:Contas não configurada.");

    opcoes.UsarConvencoesEuroBank(typeof(DebitarTransferenciaExternaManipulador).Assembly);
    opcoes.UsarInboxDuravel(connectionString);
    opcoes.UsarKafka(kafka);
    opcoes.OuvirTopicoDebezium(TiposComandosSaga.Topico, TiposComandosSaga.Mapa);
    opcoes.AplicarPoliticaDeErros();
});

// Cria as tabelas do Wolverine (inbox, dead letter) no schema "wolverine" do banco de Contas.
builder.Services.AddResourceSetupOnStartup();
builder.Services.AddHealthChecks().AddDbContextCheck<ContasDbContext>();

var app = builder.Build();

app.MapHealthChecks("/health");

await app.RunAsync();
