namespace EuroBank.Contas.Application.Commands;

/// <summary>Volta uma conta bloqueada para ativa.</summary>
public sealed record DesbloquearConta(Guid ContaId);
