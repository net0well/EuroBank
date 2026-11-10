namespace EuroBank.Transferencias.Api.TransferObjects;

/// <summary>
/// Corpo de POST /transferencias-cambio: para outra conta EuroBank, em outra moeda. moedaDoValor (padrão: a moeda da
/// conta de origem) pode ser a de origem ou a de destino.
/// </summary>
public sealed record TransferenciaCambioRequisicao(
    Guid ContaOrigemId,
    string? IbanDestino,
    decimal Valor,
    string? MoedaDoValor,
    DateTimeOffset? ExecutarEm,
    string? Descricao,
    string? EmailNotificacao);
