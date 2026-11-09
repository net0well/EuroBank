namespace EuroBank.Contas.Application.Commands;

/// <summary>Compensação pedida pela saga: devolver valor e tarifa de uma transferência já debitada.</summary>
public sealed record EstornarTransferencia(Guid TransferenciaId, Guid ContaId);
