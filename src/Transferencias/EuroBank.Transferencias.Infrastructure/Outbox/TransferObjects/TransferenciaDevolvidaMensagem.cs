namespace EuroBank.Transferencias.Infrastructure.Outbox.TransferObjects;

/// <summary>
/// Evento da transferência (eurobank.transferencias.eventos): ValorDevolvido é o que voltou à conta, sem a tarifa, na
/// Moeda da conta; valor como string decimal ("100.00").
/// </summary>
public sealed record TransferenciaDevolvidaMensagem(
    Guid TransferenciaId,
    Guid ContaOrigemId,
    string ValorDevolvido,
    string Moeda,
    ResumoTransferenciaMensagem Resumo,
    DateTimeOffset OcorridoEm);
