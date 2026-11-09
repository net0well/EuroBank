namespace EuroBank.Transferencias.Infrastructure.Outbox.TransferObjects;

/// <summary>Payload de TransferenciaCancelada e TransferenciaRecusada (o header "tipo" diz qual das duas).</summary>
public sealed record TransferenciaNaoConcluidaMensagem(
    Guid TransferenciaId,
    Guid ContaOrigemId,
    string Motivo,
    DateTimeOffset OcorridoEm);
