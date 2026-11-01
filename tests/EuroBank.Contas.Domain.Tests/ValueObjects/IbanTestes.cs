
using EuroBank.Contas.Domain.Exceptions;
using EuroBank.Contas.Domain.ValueObjects;

namespace EuroBank.Contas.Domain.Tests.ValueObjects;

public sealed class IbanTestes
{
    [Theory]
    [InlineData(1L)]
    [InlineData(42L)]
    [InlineData(9_999_999_999_999L)]
    public void Gerar_ComSequencial_ProduzIbanEuroBankValido(long sequencial)
    {
        var iban = Iban.Gerar(sequencial);

        iban.Valor.ShouldStartWith("PT");
        iban.Valor.Substring(4, 4).ShouldBe("EURO");
        iban.Valor.Length.ShouldBe(21);
        Iban.EhValido(iban.Valor).ShouldBeTrue();
    }

    [Fact]
    public void Gerar_SequenciaisDiferentes_ProduzemIbansDiferentes()
    {
        Iban.Gerar(1).ShouldNotBe(Iban.Gerar(2));
    }

    [Fact]
    public void Criar_ComIbanRealValido_Aceita()
    {
        var iban = Iban.Criar("GB82WEST12345698765432");

        iban.Valor.ShouldBe("GB82WEST12345698765432");
    }

    [Fact]
    public void Criar_NormalizaEspacosEMinusculas()
    {
        var gerado = Iban.Gerar(7);
        var digitado = string.Join(' ', gerado.Valor.Chunk(4).Select(c => new string(c))).ToLowerInvariant();

        Iban.Criar(digitado).ShouldBe(gerado);
    }

    [Theory]
    [InlineData("")]
    [InlineData("PT")]
    [InlineData("GB83WEST12345698765432")]
    [InlineData("1234WEST12345698765432")]
    public void Criar_ComIbanInvalido_LancaExcecaoIbanInvalido(string valor)
    {
        Should.Throw<ExcecaoIbanInvalido>(() => Iban.Criar(valor));
    }
}
