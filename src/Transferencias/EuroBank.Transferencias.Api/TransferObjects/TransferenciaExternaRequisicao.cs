namespace EuroBank.Transferencias.Api.TransferObjects;

/// <summary>Corpo de POST /transferencias-externas. Sem executarEm, a saga começa na hora.</summary>
public sealed record TransferenciaExternaRequisicao(
    Guid ContaOrigemId,
    string? IbanDestino,
    string? Bic,
    string? NomeBeneficiario,
    decimal Valor,
    DateTimeOffset? ExecutarEm,
    string? Descricao,
    string? EmailNotificacao);
