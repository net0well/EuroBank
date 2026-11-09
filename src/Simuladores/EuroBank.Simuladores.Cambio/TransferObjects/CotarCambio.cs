namespace EuroBank.Simuladores.Cambio.TransferObjects;

/// <summary>Pedido de cotação do EuroBank: Valor está em MoedaDoValor (a de origem ou a de destino).</summary>
public sealed record CotarCambio(Guid TransferenciaId, string MoedaOrigem, string MoedaDestino, decimal Valor, string MoedaDoValor);
