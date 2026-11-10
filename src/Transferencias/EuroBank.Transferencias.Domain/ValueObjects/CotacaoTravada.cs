namespace EuroBank.Transferencias.Domain.ValueObjects;

/// <summary>Cotação emitida pelo câmbio: Taxa é quanto de moeda de destino vale 1 da moeda de origem.</summary>
public sealed record CotacaoTravada(Guid Id, decimal Taxa, DateTimeOffset ValidaAte);
