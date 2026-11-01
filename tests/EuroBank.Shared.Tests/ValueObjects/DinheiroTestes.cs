
using EuroBank.Shared.Exceptions;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Shared.Tests.ValueObjects;

public sealed class DinheiroTestes
{
    [Theory]
    [InlineData("0")]
    [InlineData("10.5")]
    [InlineData("100.00")]
    public void De_ComValorValido_CriaEmEuro(string texto)
    {
        var valor = decimal.Parse(texto, System.Globalization.CultureInfo.InvariantCulture);

        var dinheiro = Dinheiro.De(valor);

        dinheiro.Valor.ShouldBe(valor);
        Dinheiro.Moeda.ShouldBe("EUR");
    }

    [Fact]
    public void De_ComValorNegativo_LancaExcecaoValorInvalido()
    {
        Should.Throw<ExcecaoValorInvalido>(() => Dinheiro.De(-0.01m));
    }

    [Fact]
    public void De_ComMaisDeDuasCasasDecimais_LancaExcecaoValorInvalido()
    {
        Should.Throw<ExcecaoValorInvalido>(() => Dinheiro.De(10.555m));
    }

    [Fact]
    public void ToString_SempreUsaDuasCasasComPonto()
    {
        Dinheiro.De(100m).ToString().ShouldBe("100.00");
        Dinheiro.De(0.5m).ToString().ShouldBe("0.50");
    }

    [Fact]
    public void Soma_E_Subtracao_RetornamNovoValor()
    {
        var cem = Dinheiro.De(100m);
        var trinta = Dinheiro.De(30m);

        (cem + trinta).ShouldBe(Dinheiro.De(130m));
        (cem - trinta).ShouldBe(Dinheiro.De(70m));
    }

    [Fact]
    public void Subtracao_QueFicariaNegativa_LancaExcecaoValorInvalido()
    {
        Should.Throw<ExcecaoValorInvalido>(() => Dinheiro.De(10m) - Dinheiro.De(20m));
    }

    [Fact]
    public void Igualdade_IgnoraEscalaDoDecimal()
    {
        Dinheiro.De(100m).ShouldBe(Dinheiro.De(100.00m));
    }
}
