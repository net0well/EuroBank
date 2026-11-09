namespace EuroBank.Simuladores.BancoExterno.Persistencia;

/// <summary>Resposta que o banco externo vai publicar quando chegar a hora (latência, atrasos e devoluções).</summary>
public sealed record RespostaAgendada(
    Guid Id,
    Guid TransferenciaId,
    string Tipo,
    string Payload,
    string? Traceparent,
    DateTimeOffset ResponderEm);
