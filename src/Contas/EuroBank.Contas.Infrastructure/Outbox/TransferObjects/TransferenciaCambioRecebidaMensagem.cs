namespace EuroBank.Contas.Infrastructure.Outbox.TransferObjects;

/// <summary>Evento de conta publicado para TransferenciaCambioRecebida (crédito da saga de câmbio interno).</summary>
public sealed record TransferenciaCambioRecebidaMensagem(
    Guid ContaId,
    Guid TransferenciaId,
    string IbanOrigem,
    string NomeOrigem,
    string? Descricao,
    string Valor,
    string MoedaOrigem,
    string ValorOrigem,
    string Cotacao,
    string SaldoApos,
    int Versao,
    DateTimeOffset OcorridoEm);
