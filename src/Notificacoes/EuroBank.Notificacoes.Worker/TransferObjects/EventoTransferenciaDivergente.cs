namespace EuroBank.Notificacoes.Worker.TransferObjects;

/// <summary>TransferenciaDivergente: o dinheiro não fecha e a transferência espera a reconciliação.</summary>
public sealed record EventoTransferenciaDivergente(
    Guid TransferenciaId,
    Guid ContaOrigemId,
    string Motivo,
    ResumoTransferencia? Resumo,
    DateTimeOffset OcorridoEm);
