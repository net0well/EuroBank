namespace EuroBank.Extrato.Infrastructure.Persistence.Entities;

/// <summary>
/// Linha única da tabela cache_estado: desde quando o cache do Redis está suspeito (uma escrita nele falhou e ele pode
/// ter dados velhos). Fica no Postgres para todas as instâncias do worker enxergarem a mesma marca.
/// </summary>
public sealed class MarcaCache
{
    public const int IdUnico = 1;

    public int Id { get; set; }

    public DateTimeOffset? SuspeitoDesde { get; set; }
}
