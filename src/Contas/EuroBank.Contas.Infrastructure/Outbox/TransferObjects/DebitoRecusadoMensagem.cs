namespace EuroBank.Contas.Infrastructure.Outbox.TransferObjects;

/// <summary>Resposta à saga (eurobank.contas.respostas): o débito foi recusado; nada mudou na conta.</summary>
public sealed record DebitoRecusadoMensagem(Guid TransferenciaId, Guid ContaId, string Motivo);
