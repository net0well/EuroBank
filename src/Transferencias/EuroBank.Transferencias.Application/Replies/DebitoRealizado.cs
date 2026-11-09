namespace EuroBank.Transferencias.Application.Replies;

/// <summary>Resposta de Contas (eurobank.contas.respostas): valor e tarifa debitados.</summary>
public sealed record DebitoRealizado(Guid TransferenciaId, Guid ContaId, decimal Valor, decimal Tarifa);
