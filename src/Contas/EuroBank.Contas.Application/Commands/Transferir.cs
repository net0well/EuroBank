using EuroBank.Shared.ValueObjects;

namespace EuroBank.Contas.Application.Commands;

/// <summary>
/// Transfere da conta de origem para a conta do IBAN de destino, atomicamente, depois de o antifraude aprovar (com a
/// origem do cliente, se informada).
/// </summary>
public sealed record Transferir(
    Guid ContaOrigemId, string? IbanDestino, decimal Valor, string? DispositivoId = null, Localizacao? Localizacao = null);
