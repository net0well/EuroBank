using EuroBank.Shared.Enums;

namespace EuroBank.Extrato.Domain.ValueObjects;

/// <summary>
/// O outro lado de um lançamento com câmbio: no envio, quanto o destino recebeu; no recebimento, quanto a origem
/// pagou. Cotacao é a taxa usada (moeda de destino por 1 da moeda de origem).
/// </summary>
public sealed record CambioDoLancamento(Moeda Moeda, decimal Valor, decimal Cotacao);
