using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.ValueObjects;

namespace EuroBank.Antifraude.Domain.Services;

/// <summary>
/// PSI (population stability index): para cada faixa do treino, (atual - esperado) × ln(atual / esperado), somado.
/// Faixa vazia de um lado vira 0,0001, para o logaritmo existir; uma faixa que o treino nunca viu e a produção enche
/// pesa muito, como deve.
/// </summary>
public static class CalculadoraDrift
{
    public const double Epsilon = 1e-4;

    public static double Psi(DistribuicaoFeature referencia, IReadOnlyCollection<double> valores)
    {
        ArgumentNullException.ThrowIfNull(referencia);
        ArgumentNullException.ThrowIfNull(valores);
        if (valores.Count == 0)
        {
            throw new ArgumentException("Sem valores não há PSI.", nameof(valores));
        }

        var contagem = new double[referencia.Proporcoes.Count];
        foreach (var valor in valores)
        {
            contagem[referencia.Faixa(valor)]++;
        }

        var psi = 0.0;
        for (var faixa = 0; faixa < contagem.Length; faixa++)
        {
            var atual = Math.Max(contagem[faixa] / valores.Count, Epsilon);
            var esperado = Math.Max(referencia.Proporcoes[faixa], Epsilon);
            psi += (atual - esperado) * Math.Log(atual / esperado);
        }

        return psi;
    }

    public static NivelDrift Classificar(double psi, LimitesDrift limites)
    {
        ArgumentNullException.ThrowIfNull(limites);
        return psi >= limites.Significativo
            ? NivelDrift.Significativo
            : psi >= limites.Moderado ? NivelDrift.Moderado : NivelDrift.Estavel;
    }
}
