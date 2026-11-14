namespace EuroBank.Transferencias.Infrastructure.Outbox.TransferObjects;

/// <summary>
/// Resumo da transferência dentro dos eventos finais (eurobank.transferencias.eventos), para quem notifica não
/// precisar consultar Transferencias. Valor na MoedaDoValor (o que o cliente pediu); Tarifa e ValorDebitado na
/// MoedaOrigem; ValorDestino na MoedaDestino. Valores como string decimal ("100.00"); ValorDebitado, ValorDestino e
/// Taxa ficam nulos quando a saga terminou antes da cotação.
/// </summary>
public sealed record ResumoTransferenciaMensagem(
    string Tipo,
    string NomeTitular,
    string NomeBeneficiario,
    string IbanDestino,
    string Valor,
    string MoedaDoValor,
    string Tarifa,
    string MoedaOrigem,
    string? ValorDebitado,
    string? ValorDestino,
    string MoedaDestino,
    decimal? Taxa,
    string? Descricao,
    string? EmailNotificacao);
