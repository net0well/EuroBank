namespace EuroBank.Contas.Infrastructure.Outbox.TransferObjects;

/// <summary>Payload publicado para TransferenciaRecebida (conta de destino).</summary>
public sealed record TransferenciaRecebidaMensagem(
    Guid ContaId,
    Guid TransferenciaId,
    string IbanOrigem,
    string Valor,
    string SaldoApos,
    int Versao,
    DateTimeOffset OcorridoEm);
