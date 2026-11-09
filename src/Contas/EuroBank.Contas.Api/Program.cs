using EuroBank.Contas.Api.Endpoints;
using EuroBank.Contas.Application;
using EuroBank.Contas.Application.Handlers;
using EuroBank.Contas.Application.Services;
using EuroBank.Contas.Infrastructure;
using EuroBank.Contas.Infrastructure.Persistence;
using EuroBank.Shared.Web.ErrorHandling;
using EuroBank.Shared.Web.Extensions;
using JasperFx.CodeGeneration.Model;
using Microsoft.EntityFrameworkCore;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);

builder.AdicionarObservabilidade("eurobank-contas");

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AdicionarAplicacaoContas();
builder.Services.AdicionarInfraestruturaContas();

// Wolverine só como mediator in-process: sem transporte e sem outbox próprio.
// Em Contas, quem publica no Kafka é o Debezium, lendo a tabela outbox.
builder.Host.UseWolverine(opcoes =>
{
    opcoes.Durability.Mode = DurabilityMode.MediatorOnly;
    opcoes.Discovery.IncludeAssembly(typeof(AbrirContaManipulador).Assembly);
    opcoes.Discovery.CustomizeHandlerDiscovery(consulta => consulta.Includes.WithNameSuffix("Manipulador"));

    // O DbContext é registrado por fábrica (AddDbContext), então o código gerado pelo Wolverine resolve
    // repositórios e consultas pelo container do escopo. Para o laboratório isso é aceitável.
    opcoes.ServiceLocationPolicy = ServiceLocationPolicy.AlwaysAllowed;
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<TratadorExcecoesDominio>();
builder.Services.AddHealthChecks().AddDbContextCheck<ContasDbContext>();
builder.Services.AdicionarSwaggerComApiKey("EuroBank - Contas");
builder.Services.AdicionarApiKey();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UsarApiKey();
app.UseSwagger();
app.UseSwaggerUI();

app.MapHealthChecks("/health");
app.MapearEndpointsContas();

if (app.Environment.IsDevelopment())
{
    // Laboratório: aplica as migrations e cria as contas de exemplo ao subir.
    await using var escopo = app.Services.CreateAsyncScope();
    await escopo.ServiceProvider.GetRequiredService<ContasDbContext>().Database.MigrateAsync();
    await escopo.ServiceProvider.GetRequiredService<SemeadorContas>().SemearAsync(CancellationToken.None);
}

await app.RunAsync();
