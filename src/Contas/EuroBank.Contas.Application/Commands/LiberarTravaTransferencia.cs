namespace EuroBank.Contas.Application.Commands;

/// <summary>Fim feliz da saga: a transferência deixa de travar o encerramento da conta.</summary>
public sealed record LiberarTravaTransferencia(Guid TransferenciaId, Guid ContaId);
