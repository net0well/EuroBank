using EuroBank.Extrato.Infrastructure;
using EuroBank.Extrato.Infrastructure.Messaging;
using EuroBank.Extrato.Infrastructure.Persistence;
using EuroBank.Extrato.Worker.Endpoints;
using EuroBank.Shared.Web.ErrorHandling;
using EuroBank.Shared.Web.Extensions;
using JasperFx.Resources;
using Microsoft.EntityFrameworkCore;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);

builder.AdicionarObservabilidade("eurobank-extrato");

builder.Services.AdicionarInfraestruturaExtrato();

// Consumidor Kafka (listener do Wolverine) e mediator do endpoint de consulta no mesmo processo.
builder.Host.UseWolverine(opcoes => opcoes.ConfigurarMensageriaExtrato(builder.Configuration));

// Cria as tabelas do Wolverine (inbox, dead letter) no schema "wolverine" ao subir.
builder.Services.AddResourceSetupOnStartup();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<TratadorExcecoesDominio>();
builder.Services.AddHealthChecks().AddDbContextCheck<ExtratoDbContext>();
builder.Services.AdicionarSwaggerComApiKey("EuroBank - Extrato");
builder.Services.AdicionarApiKey();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UsarApiKey();
app.UseSwagger();
app.UseSwaggerUI();

app.MapHealthChecks("/health");
app.MapearEndpointsExtratos();

if (app.Environment.IsDevelopment())
{
    // Laboratório: aplica as migrations da projeção ao subir.
    await using var escopo = app.Services.CreateAsyncScope();
    await escopo.ServiceProvider.GetRequiredService<ExtratoDbContext>().Database.MigrateAsync();
}

await app.RunAsync();
