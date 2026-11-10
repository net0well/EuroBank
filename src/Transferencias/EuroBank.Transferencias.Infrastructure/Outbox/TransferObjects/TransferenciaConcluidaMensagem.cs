namespace EuroBank.Transferencias.Infrastructure.Outbox.TransferObjects;

/// <summary>
/// Evento da transferência (eurobank.transferencias.eventos): Valor é o que saiu da conta, sem a tarifa, na Moeda da
/// conta; valores como string decimal ("100.00").
/// </summary>
public sealed record TransferenciaConcluidaMensagem(
    Guid TransferenciaId,
    Guid ContaOrigemId,
    string Valor,
    string Tarifa,
    string Moeda,
    DateTimeOffset OcorridoEm);
