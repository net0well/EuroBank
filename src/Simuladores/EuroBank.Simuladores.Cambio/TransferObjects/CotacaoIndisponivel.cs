namespace EuroBank.Simuladores.Cambio.TransferObjects;

public sealed record CotacaoIndisponivel(Guid TransferenciaId, string MoedaOrigem, string MoedaDestino, string Motivo)
    : IRespostaCambio;
