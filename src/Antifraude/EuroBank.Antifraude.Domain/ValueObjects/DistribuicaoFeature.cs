namespace EuroBank.Antifraude.Domain.ValueObjects;

/// <summary>
/// Como uma feature (ou o score, com o nome "Score") se distribuía no treino: os limites entre as faixas e a fração do
/// treino em cada uma. Há uma faixa a mais que limites (a última vai até o infinito). É a referência do drift (PSI).
/// </summary>
public sealed record DistribuicaoFeature(string Nome, IReadOnlyList<double> Limites, IReadOnlyList<double> Proporcoes)
{
    public const string NomeScore = "Score";

    /// <summary>A faixa de um valor: a primeira cujo limite é maior que ele.</summary>
    public int Faixa(double valor)
    {
        for (var i = 0; i < Limites.Count; i++)
        {
            if (valor < Limites[i])
            {
                return i;
            }
        }

        return Limites.Count;
    }
}
