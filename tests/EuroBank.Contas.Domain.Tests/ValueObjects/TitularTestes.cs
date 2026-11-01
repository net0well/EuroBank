
using EuroBank.Contas.Domain.Exceptions;
using EuroBank.Contas.Domain.ValueObjects;

namespace EuroBank.Contas.Domain.Tests.ValueObjects;

public sealed class TitularTestes
{
    [Fact]
    public void Criar_ComNome_RemoveEspacosDasPontas()
    {
        Titular.Criar("  Ana Souza ").Nome.ShouldBe("Ana Souza");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Criar_SemNome_LancaExcecaoTitularInvalido(string nome)
    {
        Should.Throw<ExcecaoTitularInvalido>(() => Titular.Criar(nome));
    }

    [Fact]
    public void Criar_ComMaisDe120Caracteres_LancaExcecaoTitularInvalido()
    {
        Should.Throw<ExcecaoTitularInvalido>(() => Titular.Criar(new string('a', 121)));
    }

    [Fact]
    public void Criar_Com120Caracteres_Aceita()
    {
        Titular.Criar(new string('a', 120)).Nome.Length.ShouldBe(120);
    }
}
