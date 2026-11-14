namespace EuroBank.Notificacoes.Worker.TransferObjects;

/// <summary>
/// TransferenciaConcluida: Valor saiu da conta (sem a tarifa), na Moeda da conta. Motivo só vem quando a conclusão
/// saiu do caminho feliz (Reconciliada). Resumo é nulo nos eventos publicados antes das notificações existirem.
/// </summary>
public sealed record EventoTransferenciaConcluida(
    Guid TransferenciaId,
    Guid ContaOrigemId,
    decimal Valor,
    decimal Tarifa,
    string Moeda,
    string? Motivo,
    ResumoTransferencia? Resumo,
    DateTimeOffset OcorridoEm);
