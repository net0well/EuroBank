namespace EuroBank.Contas.Application.Commands;

/// <summary>Transfere da conta de origem para a conta do IBAN de destino, atomicamente.</summary>
public sealed record Transferir(Guid ContaOrigemId, string? IbanDestino, decimal Valor);
