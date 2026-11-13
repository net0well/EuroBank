using EuroBank.Shared.Enums;

namespace EuroBank.Transferencias.Domain.ValueObjects;

/// <summary>
/// Uma conta EuroBank como Transferencias a conhece: pela projeção local dos eventos de Contas (id, IBAN, titular,
/// moeda e se já foi encerrada). Transferencias nunca consulta Contas diretamente.
/// </summary>
public sealed record ContaEuroBank(Guid Id, string Iban, string Titular, Moeda Moeda, bool Encerrada = false);
