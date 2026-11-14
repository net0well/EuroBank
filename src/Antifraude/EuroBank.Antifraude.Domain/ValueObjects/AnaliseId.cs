namespace EuroBank.Antifraude.Domain.ValueObjects;

public readonly record struct AnaliseId(Guid Valor)
{
    public static AnaliseId Nova() => new(Guid.CreateVersion7());

    public override string ToString() => Valor.ToString();
}
