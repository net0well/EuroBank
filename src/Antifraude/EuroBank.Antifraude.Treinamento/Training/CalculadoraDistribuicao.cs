using EuroBank.Antifraude.Domain.ValueObjects;

namespace EuroBank.Antifraude.Treinamento.Training;

/// <summary>
/// A distribuição de referência do drift: os decis de cada coluna no treino (o valor em 10%, 20%... das linhas
/// ordenadas; limites repetidos viram um só, o que acontece nas features quase sempre 0) e a fração do treino em
/// cada faixa.
/// </summary>
public static class CalculadoraDistribuicao
{
    public static DistribuicaoFeature Calcular(string nome, IReadOnlyList<double> valores)
    {
        ArgumentNullException.ThrowIfNull(valores);
        var ordenados = valores.Order().ToArray();
        var limites = Enumerable.Range(1, 9)
            .Select(decil => ordenados[Math.Max(0, (int)(decil / 10.0 * ordenados.Length) - 1)])
            .Distinct()
            .ToList();

        var referencia = new DistribuicaoFeature(nome, limites, []);
        var contagem = new double[limites.Count + 1];
        foreach (var valor in valores)
        {
            contagem[referencia.Faixa(valor)]++;
        }

        return referencia with { Proporcoes = contagem.Select(quantos => quantos / valores.Count).ToList() };
    }
}
