using System.Diagnostics.Metrics;
using EuroBank.Extrato.Infrastructure.Enums;

namespace EuroBank.Extrato.Infrastructure.Cache;

/// <summary>
/// Métricas OpenTelemetry do cache do Extrato (medidor EuroBank.Extrato.Cache): acertos, faltas, falhas do Redis,
/// duração da leitura por origem e idade do dado servido do Redis (agora menos quando o retrato foi gravado).
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
        _duracao = medidor.CreateHistogram<double>("extrato.cache.read.duration", "ms", "Duração da leitura do extrato, por origem");
        _idade = medidor.CreateHistogram<double>("extrato.cache.age", "s", "Idade do dado servido do Redis");
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
