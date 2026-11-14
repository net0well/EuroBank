namespace EuroBank.Antifraude.Domain.ValueObjects;

public readonly record struct CasoId(Guid Valor)
{
    public static CasoId Novo() => new(Guid.CreateVersion7());

    public override string ToString() => Valor.ToString();
}
