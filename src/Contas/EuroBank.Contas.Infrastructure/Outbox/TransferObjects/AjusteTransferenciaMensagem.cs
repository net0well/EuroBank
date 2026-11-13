namespace EuroBank.Contas.Infrastructure.Outbox.TransferObjects;

/// <summary>Evento de conta publicado para DebitoReconciliacao e DevolucaoTransferenciaExterna (o tipo vai no header).</summary>
public sealed record AjusteTransferenciaMensagem(
    Guid ContaId,
    Guid TransferenciaId,
    string NomeBeneficiario,
    string Valor,
    string SaldoApos,
    int Versao,
    DateTimeOffset OcorridoEm);
