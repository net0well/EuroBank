namespace EuroBank.Contas.Application.Commands;

/// <summary>Credita um valor na conta.</summary>
public sealed record Depositar(Guid ContaId, decimal Valor);
