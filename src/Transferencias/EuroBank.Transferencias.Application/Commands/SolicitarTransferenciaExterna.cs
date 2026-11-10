namespace EuroBank.Transferencias.Application.Commands;

/// <summary>
/// Pedido de transferência para um IBAN de outro banco (POST /transferencias-externas). Sem MoedaDestino, o destino
/// recebe na moeda da conta; sem MoedaDoValor, o valor está na moeda da conta.
/// </summary>
public sealed record SolicitarTransferenciaExterna(
    string? IdempotencyKey,
    Guid ContaOrigemId,
    string? IbanDestino,
    string? Bic,
    string? NomeBeneficiario,
    decimal Valor,
    DateTimeOffset? ExecutarEm,
    string? Descricao,
    string? EmailNotificacao,
    string? MoedaDoValor = null,
    string? MoedaDestino = null);
