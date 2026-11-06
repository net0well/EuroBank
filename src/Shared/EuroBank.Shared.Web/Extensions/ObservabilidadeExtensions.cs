using EuroBank.Shared.Web.Observability;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace EuroBank.Shared.Web.Extensions;

public static class ObservabilidadeExtensions
{
    private const string ModeloConsole =
        "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj} {Properties:j}{NewLine}{Exception}";

    /// <summary>Serilog estruturado no console e traces OpenTelemetry exportados via OTLP (Jaeger).</summary>
    public static WebApplicationBuilder AdicionarObservabilidade(this WebApplicationBuilder builder, string nomeServico)
    {
        builder.Services.AddSerilog((servicos, configuracao) => configuracao
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(servicos)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Servico", nomeServico)
            .WriteTo.Console(outputTemplate: ModeloConsole));

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(recurso => recurso.AddService(nomeServico))
            .WithTracing(tracing => tracing
                .SetSampler(new AmostradorSemChamadasAvulsas())
                .AddAspNetCoreInstrumentation(opcoes => opcoes.Filter = contexto =>
                    !contexto.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase))
                .AddNpgsql()
                .AddSource("Wolverine"))
            .UseOtlpExporter();

        return builder;
    }
}
