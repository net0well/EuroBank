using System.Text.Json;
using Bunit;
using EuroBank.Web.Antifraude.TransferObjects;
using EuroBank.Web.Components.Pages.Antifraude;
using EuroBank.Web.Tests.Fixtures;

namespace EuroBank.Web.Tests.Components.Antifraude;

/// <summary>O caso: o analista vê os sinais, o score e a explicação do modelo, assume e decide; só quem assumiu decide.</summary>
public sealed class PaginaCasoTestes : TestesDeTela
{
    private IRenderedComponent<PaginaCaso> Abrir(CasoDetalhe caso)
    {
        Api.Responder($"GET casos/{caso.Id}", caso);
        var tela = RenderizarTela<PaginaCaso>(new Dictionary<string, object?> { ["Id"] = caso.Id });
        tela.WaitForAssertion(() => tela.Markup.ShouldContain("Caso de revisão"));
        return tela;
    }

    [Fact]
    public void MostraSinaisScoreEExplicacaoDoModelo()
    {
        var caso = Exemplos.Caso(challenger: Exemplos.Modelo(versao: 4, probabilidade: 0.2, pontos: 10));

        var tela = Abrir(caso);

        tela.Markup.ShouldContain("score 57");
        tela.Markup.ShouldContain("ModeloFraude");
        tela.Markup.ShouldContain("Modelo ativo: versão 3");
        tela.Markup.ShouldContain("probabilidade 0.930");
        tela.Markup.ShouldContain("Challenger (sombra): versão 4");
        tela.Markup.ShouldContain("10 pontos (somaria)");
        tela.Markup.ShouldContain("LogValor");
    }

    [Fact]
    public void SemNome_PedeQueOAnalistaSeIdentifique()
    {
        var tela = Abrir(Exemplos.Caso());

        tela.Markup.ShouldContain("Informe seu nome");
        tela.FindAll("button").ShouldNotContain(botao => botao.TextContent.Contains("Assumir", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Aberto_AssumirMandaONomeDoAnalista()
    {
        await IdentificarAsync("Marta");
        var caso = Exemplos.Caso();
        Api.Responder($"POST casos/{caso.Id}/atribuicao", caso with { Estado = "EmAnalise", Analista = "Marta" });
        var tela = Abrir(caso);

        await tela.FindAll("button").Single(botao => botao.TextContent.Contains("Assumir o caso", StringComparison.Ordinal)).ClickAsync(new());

        tela.WaitForAssertion(() => tela.Markup.ShouldContain("Aprovar"));
        var pedido = Api.Pedidos.Single(pedido => pedido.Metodo == "POST");
        JsonDocument.Parse(pedido.Corpo!).RootElement.GetProperty("analista").GetString().ShouldBe("Marta");
    }

    [Fact]
    public async Task EmAnalisePorOutro_SoQuemAssumiuDecide()
    {
        await IdentificarAsync("Marta");

        var tela = Abrir(Exemplos.Caso(estado: "EmAnalise", analista: "Bruno"));

        tela.Markup.ShouldContain("Em análise por Bruno");
        tela.FindAll("button").ShouldNotContain(botao => botao.TextContent.Contains("Recusar", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DecisaoDeOutroLugar_ATelaRecarrega()
    {
        await IdentificarAsync("Marta");
        var caso = Exemplos.Caso(estado: "EmAnalise", analista: "Marta");
        var tela = Abrir(caso);
        Api.Responder($"GET casos/{caso.Id}", caso with { Estado = "Expirado" });

        Avisos.Publicar(new AvisoFila(caso.Analise.OperacaoId, null, 57, "Expirada", "Expiracao", null));

        tela.WaitForAssertion(() => tela.Markup.ShouldContain("Expirado"));
    }
}
