namespace EuroBank.Contas.Application.Commands;

/// <summary>Bloqueia a conta: ela passa a aceitar só créditos.</summary>
public sealed record BloquearConta(Guid ContaId);
