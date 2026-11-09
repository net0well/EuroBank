using EuroBank.Shared.Web.ErrorHandling;
using EuroBank.Shared.Web.Extensions;
using EuroBank.Transferencias.Api.Endpoints;
using EuroBank.Transferencias.Application;
using EuroBank.Transferencias.Application.Handlers;
using EuroBank.Transferencias.Infrastructure;
using EuroBank.Transferencias.Infrastructure.Persistence;
using JasperFx.CodeGeneration.Model;
using Microsoft.EntityFrameworkCore;
using Wolverine;

// API do orquestrador: recebe os pedidos e o cancelamento. Quem conduz a saga depois do primeiro passo é o
// Transferencias.Worker (respostas dos participantes e prazos).
var builder = WebApplication.CreateBuilder(args);

builder.AdicionarObservabilidade("eurobank-transferencias");

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AdicionarAplicacaoTransferencias();
builder.Services.AdicionarInfraestruturaTransferencias();

// Wolverine só como mediator in-process: quem publica no Kafka é o Debezium, lendo a outbox.
builder.Host.UseWolverine(opcoes =>
{
    opcoes.Durability.Mode = DurabilityMode.MediatorOnly;
    opcoes.Discovery.IncludeAssembly(typeof(SolicitarTransferenciaExternaManipulador).Assembly);
    opcoes.Discovery.CustomizeHandlerDiscovery(consulta => consulta.Includes.WithNameSuffix("Manipulador"));
    opcoes.ServiceLocationPolicy = ServiceLocationPolicy.AlwaysAllowed;
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<TratadorExcecoesDominio>();
builder.Services.AddHealthChecks().AddDbContextCheck<TransferenciasDbContext>();
builder.Services.AdicionarSwaggerComApiKey("EuroBank - Transferências");
builder.Services.AdicionarApiKey();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UsarApiKey();
app.UseSwagger();
app.UseSwaggerUI();

app.MapHealthChecks("/health");
app.MapearEndpointsTransferencias();

if (app.Environment.IsDevelopment())
{
    // Laboratório: aplica as migrations ao subir.
    await using var escopo = app.Services.CreateAsyncScope();
    await escopo.ServiceProvider.GetRequiredService<TransferenciasDbContext>().Database.MigrateAsync();
}

await app.RunAsync();
