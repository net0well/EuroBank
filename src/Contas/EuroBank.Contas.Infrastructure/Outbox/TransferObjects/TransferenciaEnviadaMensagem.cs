namespace EuroBank.Contas.Infrastructure.Outbox.TransferObjects;

/// <summary>Payload publicado para TransferenciaEnviada (conta de origem).</summary>
public sealed record TransferenciaEnviadaMensagem(
    Guid ContaId,
    Guid TransferenciaId,
    string IbanDestino,
    string Valor,
    string SaldoApos,
    int Versao,
    DateTimeOffset OcorridoEm);
