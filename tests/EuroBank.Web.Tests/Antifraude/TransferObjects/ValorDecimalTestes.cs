using EuroBank.Web.Antifraude.TransferObjects;

namespace EuroBank.Web.Tests.Antifraude.TransferObjects;

/// <summary>O decimal no formato do proto: units e nanos, com o mesmo sinal.</summary>
public sealed class ValorDecimalTestes
{
    [Theory]
    [InlineData("1234.56", 1_234, 560_000_000)]
    [InlineData("0.01", 0, 10_000_000)]
    [InlineData("1000", 1_000, 0)]
    [InlineData("-1.5", -1, -500_000_000)]
    public void De_UnitsENanos(string valor, long unidades, int nanos)
    {
        ValorDecimal.De(decimal.Parse(valor, System.Globalization.CultureInfo.InvariantCulture)).ShouldBe(new ValorDecimal(unidades, nanos));
    }
}
