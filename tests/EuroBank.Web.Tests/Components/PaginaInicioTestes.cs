using System.Net;
using Bunit;
using EuroBank.Web.Antifraude.TransferObjects;
using EuroBank.Web.Components.Pages;
using EuroBank.Web.Tests.Fixtures;

namespace EuroBank.Web.Tests.Components;

/// <summary>O início: os números do antifraude (atualizados pelos avisos da fila) e as ferramentas do laboratório.</summary>
public sealed class PaginaInicioTestes : TestesDeTela
{
    private static VisaoGeral Visao(int pendentes, Drift drift) =>
        new(Exemplos.Agora.AddDays(-1), 40, new Dictionary<string, int> { ["Aprovada"] = 36 }, 0.075m, 0.025m, pendentes, 2, 1, 4.5, [], drift);

    private static readonly Drift SemModelo = new("SemModeloAtivo", null, null);

    [Fact]
    public void MostraOsNumerosDoAntifraude_ComOsAtalhos()
    {
        Api.Responder("GET visao-geral", Visao(3, new Drift("Medido", 2, new MedicaoDrift(2, Exemplos.Agora, Exemplos.Agora.AddDays(-1), 500, 480, "Medido", "Significativo", 8.3, []))));

        var tela = RenderizarTela<PaginaInicio>();

        tela.WaitForAssertion(() => tela.Markup.ShouldContain("Casos pendentes"));
        tela.Markup.ShouldContain("v2");
        tela.Markup.ShouldContain("Significativo");
        tela.Find("a[href='antifraude']").ShouldNotBeNull();
    }

    [Fact]
    public void SemModelo_MostraASituacaoDoDrift()
    {
        Api.Responder("GET visao-geral", Visao(0, SemModelo));

        var tela = RenderizarTela<PaginaInicio>();

        tela.WaitForAssertion(() => tela.Markup.ShouldContain("SemModeloAtivo"));
        tela.Markup.ShouldContain("nenhum");
    }

    [Fact]
    public void AntifraudeFora_MostraOErro_EAsFerramentasSeguem()
    {
        Api.ResponderProblema("GET visao-geral", HttpStatusCode.ServiceUnavailable, "indisponivel", "fora do ar");

        var tela = RenderizarTela<PaginaInicio>();

        tela.WaitForAssertion(() => tela.Markup.ShouldContain("Não foi possível ler o antifraude"));
        tela.Find("a[href='http://localhost:8026/']").ShouldNotBeNull();
        tela.Find("a[href='http://localhost:8080/']").ShouldNotBeNull();
    }

    [Fact]
    public void CasoNovoNaFila_OsNumerosRecarregam()
    {
        var pendentes = 1;
        Api.ResponderCom("GET visao-geral", () => Visao(pendentes, SemModelo));
        var tela = RenderizarTela<PaginaInicio>();
        tela.WaitForAssertion(() => Api.Pedidos.Count.ShouldBe(1));

        pendentes = 2;
        Avisos.Publicar(new AvisoFila(Guid.CreateVersion7(), Guid.CreateVersion7(), 50, null, null, null));

        tela.WaitForAssertion(() => Api.Pedidos.Count.ShouldBe(2));
    }
}
