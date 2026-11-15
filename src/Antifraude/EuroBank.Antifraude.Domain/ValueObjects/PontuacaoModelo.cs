using EuroBank.Antifraude.Domain.Enums;

namespace EuroBank.Antifraude.Domain.ValueObjects;

/// <summary>
/// O que uma versão do modelo disse sobre a operação: a probabilidade de fraude, a contribuição de cada feature (a
/// explicação) e os pontos com o peso da configuração de decisão. Para o challenger, os pontos são o que ele somaria.
/// </summary>
public sealed record PontuacaoModelo(int Versao, float Probabilidade, IReadOnlyList<ContribuicaoFeature> Contribuicoes, int Pontos = 0)
{
    /// <summary>Probabilidade vezes o peso, arredondado (0,5 sobe).</summary>
    public PontuacaoModelo ComPeso(int pesoModelo) =>
        this with { Pontos = (int)Math.Round(Probabilidade * pesoModelo, MidpointRounding.AwayFromZero) };

    public SinalRegra ParaSinal() =>
        new(
            CodigoRegra.ModeloFraude,
            TipoRegra.Peso,
            Disparou: Pontos > 0,
            Pontos,
            Sombra: false,
            string.Create(System.Globalization.CultureInfo.InvariantCulture, $"Probabilidade de fraude {Probabilidade:0.000} na versão {Versao} do modelo"),
            Efeito: null);
}
