namespace EuroBank.Notificacoes.Worker.TransferObjects;

/// <summary>
/// Resumo da transferência que vem dentro de cada evento final. Valor na MoedaDoValor (o que o cliente pediu); Tarifa
/// e ValorDebitado na MoedaOrigem; ValorDestino na MoedaDestino. ValorDebitado, ValorDestino e Taxa ficam nulos quando
/// a saga terminou antes da cotação. Taxa: quanto da moeda de destino vale 1 da moeda de origem.
/// </summary>
public sealed record ResumoTransferencia(
    string Tipo,
    string NomeTitular,
    string NomeBeneficiario,
    string IbanDestino,
    decimal Valor,
    string MoedaDoValor,
    decimal Tarifa,
    string MoedaOrigem,
    decimal? ValorDebitado,
    decimal? ValorDestino,
    string MoedaDestino,
    decimal? Taxa,
    string? Descricao,
    string? EmailNotificacao);
