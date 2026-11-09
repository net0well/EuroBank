namespace EuroBank.Contas.Infrastructure.Outbox.TransferObjects;

/// <summary>Evento de conta publicado para TransferenciaCambioEnviada (débito da saga de câmbio interno).</summary>
public sealed record TransferenciaCambioEnviadaMensagem(
    Guid ContaId,
    Guid TransferenciaId,
    string IbanDestino,
    string NomeDestino,
    string? Descricao,
    string Valor,
    string Tarifa,
    string MoedaDestino,
    string ValorDestino,
    string Cotacao,
    string SaldoApos,
    int Versao,
    DateTimeOffset OcorridoEm);
