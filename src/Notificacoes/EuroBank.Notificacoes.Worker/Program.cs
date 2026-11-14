using EuroBank.Notificacoes.Worker.Extensions;
using EuroBank.Notificacoes.Worker.Messaging;
using EuroBank.Notificacoes.Worker.Persistence;
using EuroBank.Shared.Web.Extensions;
using JasperFx.Resources;
using Microsoft.EntityFrameworkCore;
using Wolverine;

// Worker de notificações: consome os eventos finais das sagas (eurobank.transferencias.eventos) e manda um e-mail
// em pt-BR para quem pediu (emailNotificacao). Falha de SMTP não afeta a saga: a mensagem espera e tenta de novo.
var builder = WebApplication.CreateBuilder(args);

builder.AdicionarObservabilidade("eurobank-notificacoes");

builder.Services.AdicionarNotificacoes(builder.Configuration);

builder.Host.UseWolverine(opcoes => opcoes.ConfigurarMensageriaNotificacoes(builder.Configuration));

// Cria as tabelas do Wolverine (inbox, dead letter) no schema "wolverine" do banco de notificações.
builder.Services.AddResourceSetupOnStartup();
builder.Services.AddHealthChecks().AddDbContextCheck<NotificacoesDbContext>();

var app = builder.Build();

app.MapHealthChecks("/health");

if (app.Environment.IsDevelopment())
{
    // Laboratório: aplica as migrations da tabela de notificações ao subir.
    await using var escopo = app.Services.CreateAsyncScope();
    await escopo.ServiceProvider.GetRequiredService<NotificacoesDbContext>().Database.MigrateAsync();
}

await app.RunAsync();
