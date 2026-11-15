using EuroBank.Extrato.Infrastructure.Enums;

namespace EuroBank.Extrato.Infrastructure.Options;

/// <summary>Seção "Cache" do appsettings. No compose, a estratégia vem de EXTRATO_CACHE_ESTRATEGIA.</summary>
public sealed class OpcoesCache
{
    public const string Secao = "Cache";

    /// <summary>Lançamentos guardados no Redis por conta: a primeira página com o tamanho padrão.</summary>
    public const int LancamentosNoCache = 50;

    public EstrategiaCache Estrategia { get; set; } = EstrategiaCache.WriteThrough;

    /// <summary>Validade de uma entrada do cache-aside.</summary>
    public TimeSpan TtlCacheAside { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Intervalo do descarregamento das pendências do write-behind no Postgres.</summary>
    public TimeSpan IntervaloDescarga { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>Pendências descarregadas por vez.</summary>
    public int LoteDescarga { get; set; } = 500;

    /// <summary>Intervalo em que o worker confere se o cache suspeito já pode ser reconstruído.</summary>
    public TimeSpan IntervaloReconstrucao { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Por quanto tempo uma instância confia na marca de suspeito que leu do Postgres.</summary>
    public TimeSpan ValidadeLeituraMarca { get; set; } = TimeSpan.FromSeconds(1);

    public bool UsaRedis => Estrategia != EstrategiaCache.Nenhuma;
}
