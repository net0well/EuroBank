namespace EuroBank.Transferencias.Application.Replies;

/// <summary>Resposta de Contas ao câmbio interno: a conta de destino foi creditada.</summary>
public sealed record CreditoRealizado(Guid TransferenciaId, Guid ContaId, decimal Valor, string Moeda);
