namespace EuroBank.Antifraude.Domain.ValueObjects;

/// <summary>A amostra mínima para medir e os limites do PSI entre estável, moderado e significativo.</summary>
public sealed record LimitesDrift
{
    public LimitesDrift(int amostraMinima, double moderado, double significativo)
    {
        if (amostraMinima < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(amostraMinima), "A amostra mínima deve ser de pelo menos uma análise.");
        }

        if (moderado <= 0 || significativo <= moderado)
        {
            throw new ArgumentException("Os limites do PSI devem ser positivos, o significativo acima do moderado.", nameof(significativo));
        }

        AmostraMinima = amostraMinima;
        Moderado = moderado;
        Significativo = significativo;
    }

    public static LimitesDrift Padrao { get; } = new(100, 0.1, 0.25);

    public int AmostraMinima { get; }

    public double Moderado { get; }

    public double Significativo { get; }
}
