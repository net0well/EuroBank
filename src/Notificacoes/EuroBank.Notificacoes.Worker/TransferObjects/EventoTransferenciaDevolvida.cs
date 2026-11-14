namespace EuroBank.Notificacoes.Worker.TransferObjects;

/// <summary>TransferenciaDevolvida: o banco externo devolveu e ValorDevolvido voltou à conta (sem a tarifa), na Moeda da conta.</summary>
public sealed record EventoTransferenciaDevolvida(
    Guid TransferenciaId,
    Guid ContaOrigemId,
    decimal ValorDevolvido,
    string Moeda,
    ResumoTransferencia? Resumo,
    DateTimeOffset OcorridoEm);
