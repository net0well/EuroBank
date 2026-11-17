using Bunit;
using EuroBank.Antifraude.Painel.Components.Pages;
using EuroBank.Antifraude.Painel.Tests.Fixtures;
using EuroBank.Antifraude.Painel.TransferObjects;

namespace EuroBank.Antifraude.Painel.Tests.Components;

/// <summary>Os modelos: a lista com as ações de cada status e a comparação champion e challenger.</summary>
public sealed class PaginaModelosTestes : TestesDeTela
{
    [Fact]
    public void AtivaEChallenger_ComparacaoLadoALado()
    {
        Api.Responder("GET modelos", new List<VersaoModeloResumo>
        {
            Exemplos.Versao(3, "Challenger", aucPr: 0.995),
            Exemplos.Versao(2, "Ativa", aucPr: 0.991),
            Exemplos.Versao(1, "Aposentada"),
        });

        var tela = RenderizarTela<PaginaModelos>();

        tela.WaitForAssertion(() => tela.Markup.ShouldContain("Champion e challenger"));
        tela.Markup.ShouldContain("Ativa (versão 2)");
        tela.Markup.ShouldContain("Challenger (versão 3)");
        tela.Markup.ShouldContain("Retirar o challenger");
        tela.FindAll("tbody tr").Count.ShouldBeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public void SoAAtiva_SemComparacao_AtivaSemBotaoDeAtivar()
    {
        Api.Responder("GET modelos", new List<VersaoModeloResumo> { Exemplos.Versao(1, "Ativa") });

        var tela = RenderizarTela<PaginaModelos>();

        tela.WaitForAssertion(() => tela.Markup.ShouldContain("Ativa"));
        tela.Markup.ShouldNotContain("Champion e challenger");
        tela.FindAll("button").ShouldNotContain(botao => botao.TextContent.Trim() == "Ativar");
    }

    [Fact]
    public void SemVersoes_ExplicaComoTreinar()
    {
        Api.Responder("GET modelos", new List<VersaoModeloResumo>());

        var tela = RenderizarTela<PaginaModelos>();

        tela.WaitForAssertion(() => tela.Markup.ShouldContain("Nenhuma versão registrada"));
    }
}
