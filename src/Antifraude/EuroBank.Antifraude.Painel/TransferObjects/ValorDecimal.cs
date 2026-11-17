namespace EuroBank.Antifraude.Painel.TransferObjects;

/// <summary>O DecimalValue do proto: 1234.56 é units 1234 e nanos 560000000 (units e nanos com o mesmo sinal).</summary>
public sealed record ValorDecimal(long Units, int Nanos)
{
    public static ValorDecimal De(decimal valor)
    {
        var unidades = decimal.Truncate(valor);
        return new ValorDecimal((long)unidades, (int)((valor - unidades) * 1_000_000_000m));
    }
}
