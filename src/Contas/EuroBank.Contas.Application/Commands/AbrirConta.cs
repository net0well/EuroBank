namespace EuroBank.Contas.Application.Commands;

/// <summary>Abre uma conta nova, ativa e com saldo zero, na moeda escolhida (padrão EUR).</summary>
public sealed record AbrirConta(string? Titular, string? Moeda = null);
