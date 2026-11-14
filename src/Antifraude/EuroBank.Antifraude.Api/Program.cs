using EuroBank.Antifraude.Api.Interceptors;
using EuroBank.Antifraude.Api.OpenApi;
using EuroBank.Antifraude.Api.Services;
using EuroBank.Antifraude.Application;
using EuroBank.Antifraude.Application.Handlers;
using EuroBank.Antifraude.Infrastructure;
using EuroBank.Antifraude.Infrastructure.Health;
using EuroBank.Antifraude.Infrastructure.Perfil;
using EuroBank.Antifraude.Infrastructure.Persistence;
using EuroBank.Antifraude.Infrastructure.Persistence.Seed;
using EuroBank.Shared.Web.ErrorHandling;
using EuroBank.Shared.Web.Extensions;
using Grpc.AspNetCore.Server;
using JasperFx.CodeGeneration.Model;
using Microsoft.EntityFrameworkCore;
using Wolverine;

// API do Antifraude: o AntifraudeService por gRPC (porta só HTTP/2) e, na porta HTTP/1.1, o mesmo serviço por JSON
// transcoding e o REST do analista. Sem TLS, o Kestrel não negocia HTTP/1.1 e HTTP/2 na mesma porta.
var builder = WebApplication.CreateBuilder(args);

builder.AdicionarObservabilidade("eurobank-antifraude");

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AdicionarAplicacaoAntifraude();
builder.Services.AdicionarInfraestruturaAntifraude();
builder.Services.AdicionarPerfilRedis();

// Wolverine só como mediator in-process: quem publica no Kafka é o Debezium, lendo a outbox.
builder.Host.UseWolverine(opcoes =>
{
    opcoes.Durability.Mode = DurabilityMode.MediatorOnly;
    opcoes.Discovery.IncludeAssembly(typeof(AvaliarOperacaoManipulador).Assembly);
    opcoes.Discovery.CustomizeHandlerDiscovery(consulta => consulta.Includes.WithNameSuffix("Manipulador"));
    opcoes.ServiceLocationPolicy = ServiceLocationPolicy.AlwaysAllowed;
});

// Erros viram status gRPC em todos os serviços; a API key vale só para o AntifraudeService (health e reflection abertos).
builder.Services.AddGrpc(opcoes => opcoes.Interceptors.Add<InterceptadorErros>())
    .AddJsonTranscoding()
    .AddServiceOptions<AntifraudeGrpcService>(opcoes => opcoes.Interceptors.Add<InterceptadorApiKey>());
builder.Services.AddGrpcReflection();
builder.Services.AddGrpcHealthChecks()
    .AddDbContextCheck<AntifraudeDbContext>()
    .AddCheck<VerificacaoRedis>("redis");

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<TratadorRequisicaoInvalida>();
builder.Services.AddExceptionHandler<TratadorExcecoesDominio>();
builder.Services.AdicionarSwaggerComApiKey("EuroBank - Antifraude");
builder.Services.ConfigureSwaggerGen(opcoes => opcoes.DocumentFilter<RotasTranscodingFilter>());
builder.Services.AdicionarApiKey();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

// Nos endpoints gRPC (inclusive os transcodificados), quem confere a key é o interceptor: aqui só o REST.
app.UseWhen(
    contexto => contexto.GetEndpoint()?.Metadata.GetMetadata<GrpcMethodMetadata>() is null,
    ramo => ramo.UsarApiKey());
app.UseSwagger();
app.UseSwaggerUI();

app.MapHealthChecks("/health");
app.MapGrpcService<AntifraudeGrpcService>();
app.MapGrpcReflectionService();
app.MapGrpcHealthChecksService();

if (app.Environment.IsDevelopment())
{
    // Laboratório: aplica as migrations e grava as regras e a configuração iniciais ao subir.
    await using var escopo = app.Services.CreateAsyncScope();
    await escopo.ServiceProvider.GetRequiredService<AntifraudeDbContext>().Database.MigrateAsync();
    await escopo.ServiceProvider.GetRequiredService<SemeadorAntifraude>().SemearAsync(CancellationToken.None);
}

await app.RunAsync();
