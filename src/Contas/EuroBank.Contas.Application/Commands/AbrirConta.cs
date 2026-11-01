namespace EuroBank.Contas.Application.Commands;

/// <summary>Abre uma conta nova, ativa e com saldo zero.</summary>
public sealed record AbrirConta(string? Titular);
