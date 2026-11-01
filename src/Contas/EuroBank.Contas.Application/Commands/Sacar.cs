namespace EuroBank.Contas.Application.Commands;

/// <summary>Debita um valor da conta.</summary>
public sealed record Sacar(Guid ContaId, decimal Valor);
