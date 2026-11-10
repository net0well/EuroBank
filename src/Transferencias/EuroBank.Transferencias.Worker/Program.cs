using EuroBank.Shared.Messaging;
using EuroBank.Shared.Web.Extensions;
using EuroBank.Transferencias.Application;
using EuroBank.Transferencias.Application.Handlers;
using EuroBank.Transferencias.Infrastructure;
using EuroBank.Transferencias.Infrastructure.Persistence;
using EuroBank.Transferencias.Worker.Messaging;
using EuroBank.Transferencias.Worker.Options;
using EuroBank.Transferencias.Worker.Services;
using JasperFx.Resources;
using Wolverine;

// Worker do orquestrador: aplica as respostas de Contas, do câmbio e do banco externo (Kafka, com inbox durável),
// mantém a projeção local de contas (eventos ContaAberta) e roda a varredura que começa as agendadas e trata os
// prazos. Os comandos seguintes saem pela outbox, como na API.
var builder = WebApplication.CreateBuilder(args);

builder.AdicionarObservabilidade("eurobank-transferencias-worker");

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AdicionarAplicacaoTransferencias();
builder.Services.AdicionarInfraestruturaTransferencias();
builder.Services.AddOptions<OpcoesVarredura>().BindConfiguration(OpcoesVarredura.Secao);
builder.Services.AddHostedService<VarreduraSagasServico>();

builder.Host.UseWolverine(opcoes =>
{
    var kafka = builder.Configuration.GetSection(OpcoesKafka.Secao).Get<OpcoesKafka>() ?? new OpcoesKafka();
    if (string.IsNullOrWhiteSpace(kafka.GrupoConsumidor))
    {
        kafka.GrupoConsumidor = "eurobank-transferencias-worker";
    }

    var connectionString = builder.Configuration.GetConnectionString("Transferencias")
        ?? throw new InvalidOperationException("ConnectionStrings:Transferencias não configurada.");

    opcoes.UsarConvencoesEuroBank(typeof(RespostasContasManipulador).Assembly);
    opcoes.UsarInboxDuravel(connectionString);
    opcoes.UsarKafka(kafka);
    opcoes.OuvirTopicoDebezium(TiposRespostasSaga.TopicoContas, TiposRespostasSaga.Contas);
    opcoes.OuvirTopicoDebezium(TiposRespostasSaga.TopicoBancoExterno, TiposRespostasSaga.BancoExterno);
    opcoes.OuvirTopicoDebezium(TiposRespostasSaga.TopicoCambio, TiposRespostasSaga.Cambio);
    opcoes.OuvirTopicoDebezium(TiposRespostasSaga.TopicoEventosContas, TiposRespostasSaga.EventosContas);
    opcoes.AplicarPoliticaDeErros();
});

// Cria as tabelas do Wolverine (inbox, dead letter) no schema "wolverine" do banco de Transferencias.
builder.Services.AddResourceSetupOnStartup();
builder.Services.AddHealthChecks().AddDbContextCheck<TransferenciasDbContext>();

var app = builder.Build();

app.MapHealthChecks("/health");

await app.RunAsync();
