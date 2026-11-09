namespace EuroBank.Contas.Infrastructure.Outbox.TransferObjects;

/// <summary>Evento de conta publicado para TransferenciaExternaEnviada (débito da saga, com a tarifa separada).</summary>
public sealed record TransferenciaExternaEnviadaMensagem(
    Guid ContaId,
    Guid TransferenciaId,
    string IbanDestino,
    string NomeBeneficiario,
    string? Descricao,
    string Valor,
    string Tarifa,
    string SaldoApos,
    int Versao,
    DateTimeOffset OcorridoEm);
