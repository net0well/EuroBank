namespace EuroBank.Transferencias.Api.TransferObjects;

/// <summary>
/// Corpo de POST /transferencias-externas. Sem executarEm, a saga começa na hora. moedaDestino (padrão: a moeda da
/// conta) diferente da conta passa pelo câmbio; moedaDoValor (padrão: a moeda da conta) diz em que moeda está o valor.
/// </summary>
public sealed record TransferenciaExternaRequisicao(
    Guid ContaOrigemId,
    string? IbanDestino,
    string? Bic,
    string? NomeBeneficiario,
    decimal Valor,
    DateTimeOffset? ExecutarEm,
    string? Descricao,
    string? EmailNotificacao,
    string? MoedaDoValor,
    string? MoedaDestino);
