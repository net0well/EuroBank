using EuroBank.Antifraude.Infrastructure;
using EuroBank.Antifraude.Infrastructure.Messaging;
using EuroBank.Antifraude.Infrastructure.Perfil;
using EuroBank.Antifraude.Infrastructure.Persistence;
using EuroBank.Antifraude.Worker.Health;
using EuroBank.Shared.Web.Extensions;
using JasperFx.Resources;
using Wolverine;

// Worker do Antifraude: mantém no Redis o perfil de comportamento de cada conta a partir dos eventos de Contas
// (é dele que as regras leem velocidade, médias, beneficiários e saldo na hora de avaliar).
var builder = WebApplication.CreateBuilder(args);

builder.AdicionarObservabilidade("eurobank-antifraude-worker");

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AdicionarInfraestruturaAntifraude();
builder.Services.AdicionarPerfilRedis();

builder.Host.UseWolverine(opcoes => opcoes.ConfigurarMensageriaAntifraudeWorker(builder.Configuration));

// Cria as tabelas do Wolverine (inbox, dead letter) no schema "wolverine" do banco do Antifraude.
builder.Services.AddResourceSetupOnStartup();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AntifraudeDbContext>()
    .AddCheck<VerificacaoRedis>("redis");

var app = builder.Build();

app.MapHealthChecks("/health");

await app.RunAsync();
