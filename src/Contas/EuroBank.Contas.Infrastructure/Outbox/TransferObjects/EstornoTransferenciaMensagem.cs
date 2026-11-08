namespace EuroBank.Contas.Infrastructure.Outbox.TransferObjects;

/// <summary>Evento de conta publicado para EstornoTransferencia (Valor = total devolvido, valor + tarifa).</summary>
public sealed record EstornoTransferenciaMensagem(
    Guid ContaId,
    Guid TransferenciaId,
    string NomeBeneficiario,
    string Valor,
    string SaldoApos,
    int Versao,
    DateTimeOffset OcorridoEm);
