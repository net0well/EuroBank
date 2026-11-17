using EuroBank.Antifraude.Application;
using EuroBank.Antifraude.Infrastructure;
using EuroBank.Antifraude.Infrastructure.Health;
using EuroBank.Antifraude.Infrastructure.Messaging;
using EuroBank.Antifraude.Infrastructure.Perfil;
using EuroBank.Antifraude.Infrastructure.Persistence;
using EuroBank.Antifraude.Worker.Observability;
using EuroBank.Antifraude.Worker.Options;
using EuroBank.Antifraude.Worker.Services;
using EuroBank.Shared.Web.Extensions;
using JasperFx.Resources;
using Wolverine;

// Worker do Antifraude: mantém no Redis o perfil de comportamento de cada conta a partir dos eventos de Contas
// (é dele que as regras leem velocidade, médias, beneficiários e saldo), expira os casos de revisão vencidos, fecha
// o caso quando o cliente cancela a transferência e mede o drift do modelo ativo.
var builder = WebApplication.CreateBuilder(args);

builder.AdicionarObservabilidade("eurobank-antifraude-worker");

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AdicionarAplicacaoAntifraude();
builder.Services.AdicionarInfraestruturaAntifraude();
builder.Services.AdicionarPerfilRedis();

builder.Services.AddOptions<OpcoesExpiracao>().BindConfiguration(OpcoesExpiracao.Secao);
builder.Services.AddSingleton<ExpiracaoCasosServico>();
builder.Services.AddHostedService(provedor => provedor.GetRequiredService<ExpiracaoCasosServico>());

builder.Services.AddOptions<OpcoesDrift>().BindConfiguration(OpcoesDrift.Secao);
builder.Services.AddSingleton<TelemetriaDrift>();
builder.Services.AddSingleton<MedicaoDriftServico>();
builder.Services.AddHostedService(provedor => provedor.GetRequiredService<MedicaoDriftServico>());

builder.Host.UseWolverine(opcoes => opcoes.ConfigurarMensageriaAntifraudeWorker(builder.Configuration));

// Cria as tabelas do Wolverine (inbox, dead letter) no schema "wolverine" do banco do Antifraude.
builder.Services.AddResourceSetupOnStartup();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AntifraudeDbContext>()
    .AddCheck<VerificacaoRedis>("redis");

var app = builder.Build();

app.MapHealthChecks("/health");

await app.RunAsync();
