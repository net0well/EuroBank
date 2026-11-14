namespace EuroBank.Notificacoes.Worker.TransferObjects;

/// <summary>TransferenciaRecusada: terminou antes de debitar (saldo, limite, câmbio indisponível...).</summary>
public sealed record EventoTransferenciaRecusada(
    Guid TransferenciaId,
    Guid ContaOrigemId,
    string Motivo,
    ResumoTransferencia? Resumo,
    DateTimeOffset OcorridoEm);
