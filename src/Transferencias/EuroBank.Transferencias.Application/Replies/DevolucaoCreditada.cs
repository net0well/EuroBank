namespace EuroBank.Transferencias.Application.Replies;

/// <summary>Resposta de Contas: o valor devolvido pelo banco externo voltou à conta, sem a tarifa.</summary>
public sealed record DevolucaoCreditada(Guid TransferenciaId, Guid ContaId, string Valor, string Moeda);
