namespace EuroBank.Simuladores.Cambio.TransferObjects;

/// <summary>Cotação travada até ValidaAte. Taxa: unidades de MoedaDestino por 1 de MoedaOrigem, já com spread.</summary>
public sealed record CotacaoEmitida(
    Guid TransferenciaId,
    Guid CotacaoId,
    string MoedaOrigem,
    string MoedaDestino,
    decimal Taxa,
    decimal ValorOrigem,
    decimal ValorDestino,
    DateTimeOffset ValidaAte) : IRespostaCambio;
