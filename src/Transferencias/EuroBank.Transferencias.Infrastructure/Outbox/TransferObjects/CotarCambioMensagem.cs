namespace EuroBank.Transferencias.Infrastructure.Outbox.TransferObjects;

/// <summary>Comando para o câmbio (eurobank.cambio.comandos): Valor está em MoedaDoValor.</summary>
public sealed record CotarCambioMensagem(Guid TransferenciaId, string MoedaOrigem, string MoedaDestino, decimal Valor, string MoedaDoValor);
