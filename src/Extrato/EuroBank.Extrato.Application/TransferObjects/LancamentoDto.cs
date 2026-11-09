namespace EuroBank.Extrato.Application.TransferObjects;

/// <summary>
/// Valores na moeda da conta. Nos lançamentos com câmbio: no envio, MoedaDestino e ValorDestino (o que o destino
/// recebeu); no recebimento, MoedaOrigem e ValorOrigem (o que a origem pagou); Cotacao nos dois.
/// </summary>
public sealed record LancamentoDto(
    string Tipo,
    decimal Valor,
    decimal? Tarifa,
    decimal SaldoApos,
    int Versao,
    DateTimeOffset OcorridoEm,
    string Descricao,
    string? MoedaDestino = null,
    decimal? ValorDestino = null,
    string? MoedaOrigem = null,
    decimal? ValorOrigem = null,
    decimal? Cotacao = null);
