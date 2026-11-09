using System.Diagnostics;

namespace EuroBank.Simuladores.Comum;

public static class Rastreamento
{
    /// <summary>Inicia uma Activity filha do traceparent recebido (quando houver), continuando o trace de quem enviou.</summary>
    public static Activity? Iniciar(ActivitySource fonte, string nome, ActivityKind tipo, string? traceparent) =>
        traceparent is not null && ActivityContext.TryParse(traceparent, null, out var pai)
            ? fonte.StartActivity(nome, tipo, pai)
            : fonte.StartActivity(nome, tipo);
}
