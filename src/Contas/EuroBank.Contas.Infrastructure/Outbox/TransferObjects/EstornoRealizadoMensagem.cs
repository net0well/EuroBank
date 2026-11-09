namespace EuroBank.Contas.Infrastructure.Outbox.TransferObjects;

/// <summary>Resposta à saga (eurobank.contas.respostas): a compensação foi feita.</summary>
public sealed record EstornoRealizadoMensagem(Guid TransferenciaId, Guid ContaId, string ValorDevolvido);
