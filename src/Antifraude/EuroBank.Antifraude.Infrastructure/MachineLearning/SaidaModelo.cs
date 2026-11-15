using Microsoft.ML.Data;

namespace EuroBank.Antifraude.Infrastructure.MachineLearning;

/// <summary>
/// O que o modelo salvo devolve: a probabilidade de fraude e quanto cada feature empurrou o score (positivo, para
/// fraude), na ordem do vetor.
/// </summary>
public sealed class SaidaModelo
{
    public float Probability { get; set; }

    public float Score { get; set; }

    [VectorType(EntradaModelo.TamanhoVetor)]
    public float[] FeatureContributions { get; set; } = [];
}
