namespace EuroBank.Contas.Infrastructure.Outbox.TransferObjects;

/// <summary>
/// Evento de conta publicado para TransferenciaExternaEnviada (débito da saga, com a tarifa separada). Com câmbio,
/// MoedaDestino, ValorDestino e Cotacao dizem o que o destino recebe.
/// </summary>
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
    DateTimeOffset OcorridoEm,
    string? MoedaDestino,
    string? ValorDestino,
    string? Cotacao);
