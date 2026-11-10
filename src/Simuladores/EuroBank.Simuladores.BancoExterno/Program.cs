using EuroBank.Shared.Web.Extensions;
using EuroBank.Simuladores.BancoExterno;
using EuroBank.Simuladores.BancoExterno.Persistencia;
using EuroBank.Simuladores.BancoExterno.Regras;
using EuroBank.Simuladores.BancoExterno.Servicos;
using EuroBank.Simuladores.Comum;
using Microsoft.Extensions.Options;

// Banco externo simulado (rede SEPA de mentira): recebe LiquidarTransferencia/ConsultarLiquidacao pelo Kafka e
// responde conforme o final do IBAN de destino. Escrito sem Wolverine e sem EF, de propósito: Confluent.Kafka e
// Npgsql puros, com inbox e agenda de respostas feitas à mão.
var builder = WebApplication.CreateBuilder(args);

builder.AdicionarObservabilidade("eurobank-banco-externo", Telemetria.NomeFonte);

builder.Services.AddOptions<OpcoesBancoExterno>().BindConfiguration(OpcoesBancoExterno.Secao);
builder.Services.AddOptions<OpcoesKafkaSimulador>()
    .BindConfiguration(OpcoesKafkaSimulador.Secao)
    .PostConfigure(kafka =>
    {
        if (string.IsNullOrWhiteSpace(kafka.GrupoConsumidor))
        {
            kafka.GrupoConsumidor = "eurobank-banco-externo";
        }
    });

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(servicos => servicos.GetRequiredService<IOptions<OpcoesKafkaSimulador>>().Value);
builder.Services.AddSingleton(servicos => new RepositorioBancoExterno(
    servicos.GetRequiredService<IConfiguration>().GetConnectionString("BancoExterno")
        ?? throw new InvalidOperationException("ConnectionStrings:BancoExterno não configurada.")));
builder.Services.AddSingleton(servicos => new RegrasLiquidacao(
    servicos.GetRequiredService<IOptions<OpcoesBancoExterno>>(), Random.Shared));
builder.Services.AddSingleton<ProcessadorComandos>();
builder.Services.AddSingleton<PublicadorKafka>();
builder.Services.AddHostedService<ConsumidorComandosBanco>();
builder.Services.AddHostedService(servicos => new EntregadorRespostas(
    servicos.GetRequiredService<RepositorioBancoExterno>(),
    servicos.GetRequiredService<PublicadorKafka>(),
    servicos.GetRequiredService<TimeProvider>(),
    Telemetria.Fonte,
    Topicos.Respostas,
    servicos.GetRequiredService<ILogger<EntregadorRespostas>>()));
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health");

await app.Services.GetRequiredService<RepositorioBancoExterno>().CriarEstruturaAsync(app.Lifetime.ApplicationStopping);

await app.RunAsync();
