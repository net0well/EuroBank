namespace EuroBank.Antifraude.Grpc;

/// <summary>
/// Conversão entre o DecimalValue do contrato (units + nanos) e o decimal do .NET, no padrão da documentação do gRPC
/// para .NET. 12345.67 é { units = 12345, nanos = 670000000 }; units e nanos têm o mesmo sinal.
/// </summary>
public sealed partial class DecimalValue
{
    private const decimal FatorNanos = 1_000_000_000m;

    public DecimalValue(long units, int nanos)
    {
        Units = units;
        Nanos = nanos;
    }

    public static implicit operator decimal(DecimalValue valor)
    {
        ArgumentNullException.ThrowIfNull(valor);
        return valor.Units + (valor.Nanos / FatorNanos);
    }

    public static implicit operator DecimalValue(decimal valor)
    {
        var units = decimal.ToInt64(decimal.Truncate(valor));
        var nanos = decimal.ToInt32((valor - units) * FatorNanos);
        return new DecimalValue(units, nanos);
    }
}
