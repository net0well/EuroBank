namespace EuroBank.Notificacoes.Worker.TransferObjects;

/// <summary>TransferenciaCancelada: terminou sem dinheiro fora da conta (cancelada pelo cliente ou estornada).</summary>
public sealed record EventoTransferenciaCancelada(
    Guid TransferenciaId,
    Guid ContaOrigemId,
    string Motivo,
    ResumoTransferencia? Resumo,
    DateTimeOffset OcorridoEm);
