using EuroBank.Shared.Enums;
using EuroBank.Shared.Exceptions;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Shared.Tests.ValueObjects;

public sealed class DinheiroTestes
{
    [Theory]
    [InlineData("0")]
    [InlineData("10.5")]
    [InlineData("100.00")]
    public void De_SemMoeda_CriaEmEuro(string texto)
    {
        var valor = decimal.Parse(texto, System.Globalization.CultureInfo.InvariantCulture);

        var dinheiro = Dinheiro.De(valor);

        dinheiro.Valor.ShouldBe(valor);
        dinheiro.Moeda.ShouldBe(Moeda.EUR);
    }

    [Fact]
    public void De_ComMoeda_GuardaAMoeda()
    {
        Dinheiro.De(10m, Moeda.USD).Moeda.ShouldBe(Moeda.USD);
        Dinheiro.ZeroEm(Moeda.BRL).ShouldBe(Dinheiro.De(0m, Moeda.BRL));
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
        Dinheiro.De(0.5m, Moeda.GBP).ToString().ShouldBe("0.50");
    }

    [Fact]
    public void Soma_E_Subtracao_RetornamNovoValorNaMesmaMoeda()
    {
        var cem = Dinheiro.De(100m, Moeda.USD);
        var trinta = Dinheiro.De(30m, Moeda.USD);

        (cem + trinta).ShouldBe(Dinheiro.De(130m, Moeda.USD));
        (cem - trinta).ShouldBe(Dinheiro.De(70m, Moeda.USD));
    }

    [Fact]
    public void Subtracao_QueFicariaNegativa_LancaExcecaoValorInvalido()
    {
        Should.Throw<ExcecaoValorInvalido>(() => Dinheiro.De(10m) - Dinheiro.De(20m));
    }

    [Fact]
    public void OperacaoEntreMoedasDiferentes_LancaExcecaoMoedaDiferente()
    {
        var euros = Dinheiro.De(10m);
        var dolares = Dinheiro.De(10m, Moeda.USD);

        Should.Throw<ExcecaoMoedaDiferente>(() => euros + dolares);
        Should.Throw<ExcecaoMoedaDiferente>(() => euros - dolares);
        Should.Throw<ExcecaoMoedaDiferente>(() => euros < dolares);
    }

    [Fact]
    public void Igualdade_IgnoraEscalaDoDecimal_MasNaoAMoeda()
    {
        Dinheiro.De(100m).ShouldBe(Dinheiro.De(100.00m));
        Dinheiro.De(100m).ShouldNotBe(Dinheiro.De(100m, Moeda.USD));
    }

    [Theory]
    [InlineData("EUR", Moeda.EUR)]
    [InlineData("usd", Moeda.USD)]
    [InlineData(" gbp ", Moeda.GBP)]
    [InlineData("BRL", Moeda.BRL)]
    public void LerMoeda_AceitaOCodigoSemDiferenciarMaiusculas(string codigo, Moeda esperada)
    {
        Moedas.Ler(codigo).ShouldBe(esperada);
    }

    [Theory]
    [InlineData("JPY")]
    [InlineData("1")]
    [InlineData("")]
    [InlineData(null)]
    public void LerMoeda_ForaDaLista_LancaExcecaoMoedaInvalida(string? codigo)
    {
        Should.Throw<ExcecaoMoedaInvalida>(() => Moedas.Ler(codigo));
    }
}
