namespace EuroBank.Transferencias.Infrastructure.Outbox.TransferObjects;

/// <summary>
/// Payload de TransferenciaCancelada, TransferenciaRecusada e TransferenciaDivergente (o header "tipo" diz qual).
/// </summary>
public sealed record TransferenciaNaoConcluidaMensagem(
    Guid TransferenciaId,
    Guid ContaOrigemId,
    string Motivo,
    DateTimeOffset OcorridoEm);
