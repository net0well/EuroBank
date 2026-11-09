namespace EuroBank.Transferencias.Application.Replies;

/// <summary>Resposta de Contas: valor e tarifa devolvidos à conta.</summary>
public sealed record EstornoRealizado(Guid TransferenciaId, Guid ContaId, decimal ValorDevolvido);
