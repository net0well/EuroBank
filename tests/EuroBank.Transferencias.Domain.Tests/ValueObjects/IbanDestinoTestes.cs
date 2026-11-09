using EuroBank.Transferencias.Domain.Exceptions;
using EuroBank.Transferencias.Domain.ValueObjects;

namespace EuroBank.Transferencias.Domain.Tests.ValueObjects;

public sealed class IbanDestinoTestes
{
    [Fact]
    public void Criar_NormalizaEspacosEMinusculas()
    {
        IbanDestino.Criar("de89 3704 0044 0532 0130 00").Valor.ShouldBe("DE89370400440532013000");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("DE89")]
    [InlineData("8989370400440532013000")]
    [InlineData("DE89-3704-0044-0532-0130-00")]
    public void Criar_ForaDoFormato_Recusa(string? valor)
    {
        Should.Throw<ExcecaoIbanDestinoInvalido>(() => IbanDestino.Criar(valor));
    }

    [Fact]
    public void Criar_NaoConfereODigitoVerificador_ParaOsFinaisDoSimuladorFuncionarem()
    {
        IbanDestino.Criar("DE89370400440532013099").Valor.ShouldEndWith("99");
    }

    [Theory]
    [InlineData("PT50EURO0000000000001", true)]
    [InlineData("DE89370400440532013000", false)]
    [InlineData("PT50BANK0000000000001", false)]
    public void EhEuroBank_OlhaPaisECodigoDoBanco(string valor, bool esperado)
    {
        IbanDestino.Criar(valor).EhEuroBank.ShouldBe(esperado);
    }
}
