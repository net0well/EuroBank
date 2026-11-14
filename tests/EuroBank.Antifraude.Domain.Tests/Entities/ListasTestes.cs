using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Exceptions;
using EuroBank.Antifraude.Domain.ValueObjects;
using static EuroBank.Antifraude.Domain.Tests.Cenarios;

namespace EuroBank.Antifraude.Domain.Tests.Entities;

/// <summary>IBANs bloqueados e países de risco: normalizados na entrada, comparados sem diferença de caixa.</summary>
public sealed class ListasTestes
{
    [Fact]
    public void IbanBloqueado_GuardaNormalizado()
    {
        var bloqueado = IbanBloqueado.Criar(" de89 3704 0044 0532 0130 66 ", "Mula confirmada", "Marta", Agora);

        bloqueado.Iban.ShouldBe("DE89370400440532013066");
        bloqueado.Motivo.ShouldBe("Mula confirmada");
        bloqueado.BloqueadoPor.ShouldBe("Marta");
        bloqueado.BloqueadoEm.ShouldBe(Agora);
    }

    [Theory]
    [InlineData("")]
    [InlineData("DE89")]
    [InlineData("1234567890123456")]
    public void IbanBloqueado_Invalido_EhRecusado(string iban)
    {
        Should.Throw<ExcecaoIbanInvalido>(() => IbanBloqueado.Criar(iban, "x", "Marta", Agora));
    }

    [Fact]
    public void PaisRisco_GuardaEmMaiusculas()
    {
        var pais = PaisRisco.Criar("ir", Agora);

        pais.Codigo.ShouldBe("IR");
        pais.IncluidoEm.ShouldBe(Agora);
    }

    [Theory]
    [InlineData("")]
    [InlineData("I")]
    [InlineData("IRN")]
    [InlineData("1R")]
    public void PaisRisco_ForaDoFormatoDeDuasLetras_EhRecusado(string codigo)
    {
        Should.Throw<ExcecaoPaisInvalido>(() => PaisRisco.Criar(codigo, Agora));
    }

    [Fact]
    public void Listas_ComparamSemDiferencaDeCaixaNemEspacos()
    {
        var listas = Listas(bloqueados: ["DE89370400440532013066"], paises: ["IR"]);

        listas.EstaBloqueado("de89 3704 0044 0532 0130 66").ShouldBeTrue();
        listas.EhPaisDeRisco("ir").ShouldBeTrue();
        listas.EstaBloqueado(null).ShouldBeFalse();
        ListasRisco.Vazias.EhPaisDeRisco("IR").ShouldBeFalse();
    }
}
