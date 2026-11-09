namespace EuroBank.Transferencias.Application.Commands;

/// <summary>Pedido de transferência para um IBAN de outro banco (POST /transferencias-externas).</summary>
public sealed record SolicitarTransferenciaExterna(
    string? IdempotencyKey,
    Guid ContaOrigemId,
    string? IbanDestino,
    string? Bic,
    string? NomeBeneficiario,
    decimal Valor,
    DateTimeOffset? ExecutarEm,
    string? Descricao,
    string? EmailNotificacao);
