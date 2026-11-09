namespace EuroBank.Transferencias.Infrastructure.Outbox.TransferObjects;

/// <summary>Evento da transferência (eurobank.transferencias.eventos); valores como string decimal ("100.00").</summary>
public sealed record TransferenciaConcluidaMensagem(
    Guid TransferenciaId,
    Guid ContaOrigemId,
    string Valor,
    string Tarifa,
    DateTimeOffset OcorridoEm);
