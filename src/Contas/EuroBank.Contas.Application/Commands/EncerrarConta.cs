namespace EuroBank.Contas.Application.Commands;

/// <summary>Encerra a conta (só com saldo zero).</summary>
public sealed record EncerrarConta(Guid ContaId);
