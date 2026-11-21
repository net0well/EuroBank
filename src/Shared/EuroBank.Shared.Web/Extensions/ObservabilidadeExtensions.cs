using EuroBank.Shared.Web.Observability;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace EuroBank.Shared.Web.Extensions;

public static class ObservabilidadeExtensions
{
    private const string ModeloConsole =
        "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj} {Properties:j}{NewLine}{Exception}";

    /// <summary>Com esta variável, as métricas vão para um destino próprio (no compose, o Prometheus).</summary>
    public const string VariavelEndpointMetricas = "OTEL_EXPORTER_OTLP_METRICS_ENDPOINT";

    /// <summary>Os medidores do EuroBank (cache do Extrato, modelo e drift do Antifraude) e os que vierem.</summary>
    public const string MedidoresEuroBank = "EuroBank.*";

    /// <summary>
    /// Serilog estruturado no console, traces OpenTelemetry via OTLP (Jaeger) e, quando há
    /// <see cref="VariavelEndpointMetricas"/>, métricas também: as do ASP.NET Core e as dos medidores EuroBank.*.
    /// Sem ela, nada de métricas: o OTLP padrão é o Jaeger, que não as recebe.
    /// </summary>
    /// <param name="fontesAdicionais">ActivitySources próprias do serviço, além de ASP.NET Core, Npgsql e Wolverine.</param>
    public static WebApplicationBuilder AdicionarObservabilidade(
        this WebApplicationBuilder builder,
        string nomeServico,
        params string[] fontesAdicionais)
    {
        builder.Services.AddSerilog((servicos, configuracao) => configuracao
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(servicos)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Servico", nomeServico)
            .WriteTo.Console(outputTemplate: ModeloConsole));

        var telemetria = builder.Services.AddOpenTelemetry()
            .ConfigureResource(recurso => recurso.AddService(nomeServico))
            .WithTracing(tracing => tracing
                .SetSampler(new AmostradorSemChamadasAvulsas())
                .AddAspNetCoreInstrumentation(opcoes => opcoes.Filter = contexto =>
                    !contexto.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase))
                .AddNpgsql()
                .AddSource("Wolverine")
                .AddSource(fontesAdicionais));
        if (!string.IsNullOrWhiteSpace(builder.Configuration[VariavelEndpointMetricas]))
        {
            telemetria.WithMetrics(metricas => metricas
                .AddAspNetCoreInstrumentation()
                .AddMeter(MedidoresEuroBank));
        }

        telemetria.UseOtlpExporter();
        return builder;
    }
}
