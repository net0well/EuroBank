namespace EuroBank.Transferencias.Application.TransferObjects;

/// <summary>Cotação travada: 1 da moeda de origem vale Taxa da moeda de destino, até ValidaAte.</summary>
public sealed record CotacaoDto(Guid Id, decimal Taxa, DateTimeOffset ValidaAte);
