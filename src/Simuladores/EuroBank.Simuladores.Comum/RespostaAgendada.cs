namespace EuroBank.Simuladores.Comum;

/// <summary>Resposta que um simulador vai publicar quando chegar a hora (latência, atrasos e respostas tardias).</summary>
public sealed record RespostaAgendada(
    Guid Id,
    Guid TransferenciaId,
    string Tipo,
    string Payload,
    string? Traceparent,
    DateTimeOffset ResponderEm);
