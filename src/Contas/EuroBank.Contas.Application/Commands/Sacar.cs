using EuroBank.Shared.ValueObjects;

namespace EuroBank.Contas.Application.Commands;

/// <summary>Debita um valor da conta, depois de o antifraude aprovar (com a origem do cliente, se informada).</summary>
public sealed record Sacar(Guid ContaId, decimal Valor, string? DispositivoId = null, Localizacao? Localizacao = null);
