namespace EuroBank.Transferencias.Application.TransferObjects;

/// <summary>
/// Valor na moeda em que foi pedido (MoedaDoValor); Tarifa e ValorDebitado na moeda da conta (MoedaOrigem);
/// ValorDestino na moeda de destino. Com câmbio, ValorDebitado, ValorDestino e Cotacao aparecem depois da cotação.
/// AnaliseRiscoId e CasoRevisaoId aparecem quando o antifraude responde (o caso só quando foi para revisão).
/// </summary>
public sealed record TransferenciaDto(
    Guid Id,
    string Tipo,
    Guid ContaOrigemId,
    string MoedaOrigem,
    Guid? ContaDestinoId,
    string IbanDestino,
    string? Bic,
    string NomeBeneficiario,
    decimal Valor,
    string MoedaDoValor,
    decimal Tarifa,
    decimal? ValorDebitado,
    decimal? ValorDestino,
    string MoedaDestino,
    CotacaoDto? Cotacao,
    string? Descricao,
    string? EmailNotificacao,
    DateTimeOffset SolicitadaEm,
    DateTimeOffset? ExecutarEm,
    DateTimeOffset ExecutarEmEfetivo,
    string Estado,
    string? Motivo,
    bool CancelamentoSolicitado,
    Guid? AnaliseRiscoId,
    Guid? CasoRevisaoId,
    int TentativasNoPasso,
    DateTimeOffset? PrazoDoPasso,
    int Versao,
    DateTimeOffset AtualizadaEm,
    IReadOnlyList<PassoTransferenciaDto> Historico);
