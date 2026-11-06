using System.Diagnostics;
using OpenTelemetry.Trace;

namespace EuroBank.Shared.Web.Observability;

/// <summary>
/// Descarta spans de chamadas de saída (ActivityKind.Client) que não pertencem a uma requisição ou mensagem
/// registrada, como as consultas periódicas do Wolverine ao Postgres (sem pai, ou com um pai interno que não é
/// exportado). Sem isso, o Jaeger fica cheio de traces de uma consulta só e esconde os que interessam.
/// </summary>
public sealed class AmostradorSemChamadasAvulsas : Sampler
{
    private static readonly SamplingResult Descartar = new(SamplingDecision.Drop);
    private static readonly SamplingResult Registrar = new(SamplingDecision.RecordAndSample);

    public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
    {
        var pai = samplingParameters.ParentContext;
        var paiRegistrado = pai.TraceId != default && pai.TraceFlags.HasFlag(ActivityTraceFlags.Recorded);

        return samplingParameters.Kind == ActivityKind.Client && !paiRegistrado ? Descartar : Registrar;
    }
}
