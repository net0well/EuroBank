using EuroBank.Antifraude.Domain.ValueObjects;

namespace EuroBank.Antifraude.Treinamento.Training;

/// <summary>
/// Precisão, recall e taxa sinalizada em cada limiar de probabilidade, e o limiar recomendado pela meta: o mais alto
/// (menos operações sinalizadas) que ainda alcança o recall da meta. Se nenhum alcança, o de melhor F1.
/// </summary>
public static class AvaliadorLimiares
{
    public sealed record Escolha(double Limiar, bool MetaAtingida, MatrizConfusao Matriz, double Precisao, double Recall, double F1);

    /// <summary>A curva para mostrar, de 0,05 em 0,05.</summary>
    public static IReadOnlyList<PontoCurva> Curva(IReadOnlyList<(bool Fraude, float Probabilidade)> previsoes) =>
        Enumerable.Range(1, 19).Select(passo => Ponto(previsoes, passo / 20.0)).ToList();

    public static Escolha Recomendar(IReadOnlyList<(bool Fraude, float Probabilidade)> previsoes, double metaRecall, double metaTaxaSinalizada)
    {
        ArgumentNullException.ThrowIfNull(previsoes);
        var candidatos = Enumerable.Range(1, 99).Select(passo => passo / 100.0).ToList();
        var comRecall = candidatos.Where(limiar => Ponto(previsoes, limiar).Recall >= metaRecall).ToList();

        var limiar = comRecall.Count > 0
            ? comRecall.Max()
            : candidatos.MaxBy(candidato => F1(Ponto(previsoes, candidato)));
        var ponto = Ponto(previsoes, limiar);
        var matriz = Matriz(previsoes, limiar);
        return new Escolha(
            limiar,
            MetaAtingida: comRecall.Count > 0 && ponto.TaxaSinalizada <= metaTaxaSinalizada,
            matriz,
            ponto.Precisao,
            ponto.Recall,
            F1(ponto));
    }

    /// <summary>A matriz num limiar. A comparação é em float, como o modelo devolve: 0,95f fica abaixo de 0,95 em double.</summary>
    public static MatrizConfusao Matriz(IReadOnlyList<(bool Fraude, float Probabilidade)> previsoes, double limiar)
    {
        ArgumentNullException.ThrowIfNull(previsoes);
        var corte = (float)limiar;
        var vp = previsoes.Count(previsao => previsao.Fraude && previsao.Probabilidade >= corte);
        var fp = previsoes.Count(previsao => !previsao.Fraude && previsao.Probabilidade >= corte);
        var fn = previsoes.Count(previsao => previsao.Fraude && previsao.Probabilidade < corte);
        return new MatrizConfusao(vp, fp, previsoes.Count - vp - fp - fn, fn);
    }

    private static PontoCurva Ponto(IReadOnlyList<(bool Fraude, float Probabilidade)> previsoes, double limiar)
    {
        var matriz = Matriz(previsoes, limiar);
        var sinalizadas = matriz.VerdadeirosPositivos + matriz.FalsosPositivos;
        var fraudes = matriz.VerdadeirosPositivos + matriz.FalsosNegativos;
        return new PontoCurva(
            limiar,
            Precisao: sinalizadas == 0 ? 0 : (double)matriz.VerdadeirosPositivos / sinalizadas,
            Recall: fraudes == 0 ? 0 : (double)matriz.VerdadeirosPositivos / fraudes,
            TaxaSinalizada: previsoes.Count == 0 ? 0 : (double)sinalizadas / previsoes.Count);
    }

    private static double F1(PontoCurva ponto) =>
        ponto.Precisao + ponto.Recall == 0 ? 0 : 2 * ponto.Precisao * ponto.Recall / (ponto.Precisao + ponto.Recall);
}
