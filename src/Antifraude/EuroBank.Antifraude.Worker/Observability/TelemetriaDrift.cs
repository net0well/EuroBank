using System.Diagnostics.Metrics;
using EuroBank.Antifraude.Application.TransferObjects;

namespace EuroBank.Antifraude.Worker.Observability;

/// <summary>
/// Métricas OpenTelemetry do drift (medidor EuroBank.Antifraude.Drift), da última medição: o PSI de cada feature e do
/// score (tags feature e versao) e o tamanho da amostra. Sem versão ativa ou com amostra insuficiente, sem PSI. O
/// alerta no Grafana (fase de métricas) olha antifraude.drift.psi acima de 0,25.
/// </summary>
public sealed class TelemetriaDrift
{
    public const string NomeMedidor = "EuroBank.Antifraude.Drift";

    private volatile MedicaoDriftDto? _ultima;

    public TelemetriaDrift(IMeterFactory fabrica)
    {
        ArgumentNullException.ThrowIfNull(fabrica);
        var medidor = fabrica.Create(NomeMedidor);
        medidor.CreateObservableGauge("antifraude.drift.psi", Psis, description: "PSI de cada feature e do score na última medição");
        medidor.CreateObservableGauge("antifraude.drift.sample", Amostra, description: "Análises com features na janela da última medição");
    }

    public MedicaoDriftDto? Ultima => _ultima;

    public void Registrar(MedicaoDriftDto? medicao) => _ultima = medicao;

    private IEnumerable<Measurement<double>> Psis() =>
        _ultima is { } medicao
            ? medicao.Psis.Select(psi => new Measurement<double>(
                psi.Psi, new KeyValuePair<string, object?>("feature", psi.Nome), new KeyValuePair<string, object?>("versao", medicao.VersaoModelo)))
            : [];

    private IEnumerable<Measurement<int>> Amostra() =>
        _ultima is { } medicao
            ? [new Measurement<int>(medicao.Amostra, new KeyValuePair<string, object?>("versao", medicao.VersaoModelo))]
            : [];
}
