namespace EuroBank.Transferencias.Infrastructure.Outbox.TransferObjects;

/// <summary>
/// Comandos para Contas que só identificam a transferência e a conta (estornar, liberar a trava, debitar a
/// reconciliação, creditar a devolução).
/// </summary>
public sealed record ComandoContaMensagem(Guid TransferenciaId, Guid ContaId);
