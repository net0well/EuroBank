using System.Diagnostics.Metrics;

namespace EuroBank.Antifraude.Infrastructure.MachineLearning;

/// <summary>
/// Métricas OpenTelemetry do modelo em produção (medidor EuroBank.Antifraude.Modelo): pontuações e falhas, por papel
/// (ativa ou challenger) e versão. Uma falha não derruba a avaliação: ela segue só com as regras.
/// </summary>
public sealed class TelemetriaModelo
{
    public const string NomeMedidor = "EuroBank.Antifraude.Modelo";

    private readonly Counter<long> _pontuacoes;
    private readonly Counter<long> _falhas;

    public TelemetriaModelo(IMeterFactory fabrica)
    {
        ArgumentNullException.ThrowIfNull(fabrica);
        var medidor = fabrica.Create(NomeMedidor);
        _pontuacoes = medidor.CreateCounter<long>("antifraude.modelo.scores", description: "Operações pontuadas pelo modelo");
        _falhas = medidor.CreateCounter<long>("antifraude.modelo.failures", description: "Falhas ao carregar ou ao pontuar");
    }

    public void Pontuacao(string papel, int versao) =>
        _pontuacoes.Add(1, new KeyValuePair<string, object?>("papel", papel), new KeyValuePair<string, object?>("versao", versao));

    public void Falha(string papel, string operacao) =>
        _falhas.Add(1, new KeyValuePair<string, object?>("papel", papel), new KeyValuePair<string, object?>("operacao", operacao));
}
