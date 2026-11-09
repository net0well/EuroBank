using EuroBank.Shared.Web.Extensions;
using EuroBank.Simuladores.Cambio;
using EuroBank.Simuladores.Cambio.Persistencia;
using EuroBank.Simuladores.Cambio.Regras;
using EuroBank.Simuladores.Cambio.Servicos;
using EuroBank.Simuladores.Comum;
using Microsoft.Extensions.Options;

// Câmbio simulado (um provedor de cotações "de fora"): recebe CotarCambio pelo Kafka e responde com uma cotação
// travada por 10 minutos, ou com CotacaoIndisponivel. Mesmo estilo do banco externo: Confluent.Kafka e Npgsql
// puros, com inbox e agenda de respostas feitas à mão.
var builder = WebApplication.CreateBuilder(args);

builder.AdicionarObservabilidade("eurobank-cambio", Telemetria.NomeFonte);

builder.Services.AddOptions<OpcoesCambio>().BindConfiguration(OpcoesCambio.Secao);
builder.Services.AddOptions<OpcoesKafkaSimulador>()
    .BindConfiguration(OpcoesKafkaSimulador.Secao)
    .PostConfigure(kafka =>
    {
        if (string.IsNullOrWhiteSpace(kafka.GrupoConsumidor))
        {
            kafka.GrupoConsumidor = "eurobank-cambio";
        }
    });

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(servicos => servicos.GetRequiredService<IOptions<OpcoesKafkaSimulador>>().Value);
builder.Services.AddSingleton(servicos => new RepositorioCambio(
    servicos.GetRequiredService<IConfiguration>().GetConnectionString("Cambio")
        ?? throw new InvalidOperationException("ConnectionStrings:Cambio não configurada.")));
builder.Services.AddSingleton<TabelaCambio>();
builder.Services.AddSingleton<ProcessadorComandosCambio>();
builder.Services.AddSingleton<PublicadorKafka>();
builder.Services.AddHostedService<ConsumidorComandosCambio>();
builder.Services.AddHostedService(servicos => new EntregadorRespostas(
    servicos.GetRequiredService<RepositorioCambio>(),
    servicos.GetRequiredService<PublicadorKafka>(),
    servicos.GetRequiredService<TimeProvider>(),
    Telemetria.Fonte,
    Topicos.Respostas,
    servicos.GetRequiredService<ILogger<EntregadorRespostas>>()));
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health");

await app.Services.GetRequiredService<RepositorioCambio>().CriarEstruturaAsync(app.Lifetime.ApplicationStopping);

await app.RunAsync();
