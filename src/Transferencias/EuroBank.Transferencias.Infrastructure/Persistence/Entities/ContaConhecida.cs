using EuroBank.Shared.Enums;

namespace EuroBank.Transferencias.Infrastructure.Persistence.Entities;

/// <summary>
/// Linha da projeção local de contas (tabela contas_conhecidas), alimentada pelos eventos ContaAberta e
/// ContaEncerrada.
/// </summary>
public sealed class ContaConhecida
{
    public Guid Id { get; set; }

    public string Iban { get; set; } = string.Empty;

    public string Titular { get; set; } = string.Empty;

    public Moeda Moeda { get; set; }

    public DateTimeOffset AbertaEm { get; set; }

    public DateTimeOffset? EncerradaEm { get; set; }
}
