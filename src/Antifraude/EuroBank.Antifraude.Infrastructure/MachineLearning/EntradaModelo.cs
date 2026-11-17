using EuroBank.Antifraude.Features.ValueObjects;
using Microsoft.ML.Data;

namespace EuroBank.Antifraude.Infrastructure.MachineLearning;

/// <summary>
/// Uma linha como o ML.NET a recebe, no treino e na produção: o vetor de features, o rótulo (só no treino) e o peso
/// do exemplo (a classe rara pesa mais no treino).
/// </summary>
public sealed class EntradaModelo
{
    /// <summary>O tamanho do vetor; o atributo exige uma constante, e um teste a confere contra o VetorFeatures.</summary>
    public const int TamanhoVetor = 11;

    [VectorType(TamanhoVetor)]
    public float[] Features { get; set; } = new float[TamanhoVetor];

    public bool Label { get; set; }

    public float Peso { get; set; } = 1;

    public static EntradaModelo De(VetorFeatures features, bool fraude = false, float peso = 1)
    {
        ArgumentNullException.ThrowIfNull(features);
        return new EntradaModelo { Features = features.ParaArray(), Label = fraude, Peso = peso };
    }
}
