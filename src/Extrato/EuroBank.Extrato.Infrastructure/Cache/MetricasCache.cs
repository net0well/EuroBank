using System.Diagnostics.Metrics;
using EuroBank.Extrato.Infrastructure.Enums;

namespace EuroBank.Extrato.Infrastructure.Cache;

/// <summary>
/// Métricas OpenTelemetry do cache do Extrato (medidor EuroBank.Extrato.Cache): acertos, faltas, falhas do Redis,
/// duração da leitura por origem e idade do dado servido do Redis (agora menos quando o retrato foi gravado). As faixas
/// dos histogramas vão no próprio instrumento: as padrão do OpenTelemetry (0, 5, 10, 25 ms...) juntariam toda leitura
/// do Redis na primeira, e toda idade abaixo de 5 s também.
/// </summary>
public sealed class MetricasCache
{
    public const string NomeMedidor = "EuroBank.Extrato.Cache";

    private readonly Counter<long> _acertos;
    private readonly Counter<long> _faltas;
    private readonly Counter<long> _falhas;
    private readonly Histogram<double> _duracao;
    private readonly Histogram<double> _idade;

    public MetricasCache(IMeterFactory fabrica)
    {
        ArgumentNullException.ThrowIfNull(fabrica);
        var medidor = fabrica.Create(NomeMedidor);
        _acertos = medidor.CreateCounter<long>("extrato.cache.hits", description: "Leituras servidas do Redis");
        _faltas = medidor.CreateCounter<long>("extrato.cache.misses", description: "Leituras que não acharam a conta no Redis");
        _falhas = medidor.CreateCounter<long>("extrato.cache.failures", description: "Operações no Redis que falharam");
        _duracao = medidor.CreateHistogram(
            "extrato.cache.read.duration",
            "ms",
            "Duração da leitura do extrato, por origem",
            tags: null,
            new InstrumentAdvice<double> { HistogramBucketBoundaries = [0.25, 0.5, 1, 2, 5, 10, 25, 50, 100, 250, 500, 1000] });
        _idade = medidor.CreateHistogram(
            "extrato.cache.age",
            "s",
            "Idade do dado servido do Redis",
            tags: null,
            new InstrumentAdvice<double> { HistogramBucketBoundaries = [0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10, 30, 60, 300, 900, 3600] });
    }

    public void Acerto(EstrategiaCache estrategia, TimeSpan idade)
    {
        _acertos.Add(1, Tag(estrategia));
        _idade.Record(Math.Max(0, idade.TotalSeconds), Tag(estrategia));
    }

    public void Falta(EstrategiaCache estrategia) => _faltas.Add(1, Tag(estrategia));

    public void Falha(EstrategiaCache estrategia, string operacao) =>
        _falhas.Add(1, Tag(estrategia), new KeyValuePair<string, object?>("operacao", operacao));

    public void Leitura(EstrategiaCache estrategia, string origem, TimeSpan duracao) =>
        _duracao.Record(duracao.TotalMilliseconds, Tag(estrategia), new KeyValuePair<string, object?>("origem", origem));

    private static KeyValuePair<string, object?> Tag(EstrategiaCache estrategia) => new("estrategia", estrategia.ToString());
}
