namespace EuroBank.Contas.Infrastructure.Outbox.TransferObjects;

/// <summary>Resposta à saga (eurobank.contas.respostas): o débito foi feito.</summary>
public sealed record DebitoRealizadoMensagem(Guid TransferenciaId, Guid ContaId, string Valor, string Tarifa);
