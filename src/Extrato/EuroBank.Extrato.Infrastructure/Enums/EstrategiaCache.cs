namespace EuroBank.Extrato.Infrastructure.Enums;

/// <summary>
/// Como o Extrato usa o Redis para o resumo e a primeira página de cada conta (seção "Cache", chave "Estrategia").
/// As páginas seguintes, e qualquer leitura com o cache suspeito, vêm do Postgres.
/// </summary>
public enum EstrategiaCache
{
    /// <summary>Sem Redis: lê e grava só no Postgres.</summary>
    Nenhuma = 0,

    /// <summary>A leitura que não acha a conta no Redis lê do Postgres e grava com TTL; cada evento apaga a entrada.</summary>
    CacheAside = 1,

    /// <summary>Cada evento grava no Postgres e, em seguida, no Redis: a leitura logo depois já vem atualizada do cache.</summary>
    WriteThrough = 2,

    /// <summary>Cada evento grava só no Redis (estado e pendência); um serviço descarrega as pendências no Postgres em lote.</summary>
    WriteBehind = 3,
}
