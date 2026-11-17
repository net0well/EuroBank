using EuroBank.Antifraude.Painel.TransferObjects;
using EuroBank.Antifraude.Painel.ViewModels;

namespace EuroBank.Antifraude.Painel.Tests.ViewModels;

/// <summary>A regra em edição: sabe se mudou e manda no ajuste só os parâmetros alterados.</summary>
public sealed class RegraEditavelTestes
{
    private static Regra Velocidade() =>
        new("Velocidade", "Peso", "Muitas transferências em pouco tempo", 20, null, true, false,
            new Dictionary<string, decimal> { ["quantidade"] = 3, ["janelaMinutos"] = 10 }, DateTimeOffset.UtcNow);

    [Fact]
    public void SemMudanca_NaoAlterada()
    {
        RegraEditavel.De(Velocidade()).Alterada.ShouldBeFalse();
    }

    [Fact]
    public void ParametroAlterado_SoEleNoAjuste()
    {
        var regra = RegraEditavel.De(Velocidade());
        regra.Parametros["quantidade"] = 5;
        regra.Sombra = true;

        regra.Alterada.ShouldBeTrue();
        var ajuste = regra.ParaAjuste();
        ajuste.Peso.ShouldBe(20);
        ajuste.Sombra.ShouldBeTrue();
        ajuste.Parametros.ShouldNotBeNull().ShouldBe(new Dictionary<string, decimal> { ["quantidade"] = 5 });
    }

    [Fact]
    public void RegraDura_SemPeso()
    {
        var bloqueio = new Regra("ListaBloqueio", "Dura", "IBAN bloqueado", 0, "Recusar", true, false, new Dictionary<string, decimal>(), DateTimeOffset.UtcNow);

        RegraEditavel.De(bloqueio).Dura.ShouldBeTrue();
    }
}
