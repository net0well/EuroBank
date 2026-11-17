using Bunit;
using EuroBank.Antifraude.Painel.Components.Pages;
using EuroBank.Antifraude.Painel.Tests.Fixtures;
using EuroBank.Antifraude.Painel.TransferObjects;
using EuroBank.Shared.TransferObjects;

namespace EuroBank.Antifraude.Painel.Tests.Components;

/// <summary>A fila: mostra os pendentes e, quando um evento chega pelo Kafka, recarrega sozinha (sem recarregar a página).</summary>
public sealed class PaginaCasosTestes : TestesDeTela
{
    [Fact]
    public void Abre_ComOsPendentes()
    {
        var caso = Exemplos.Resumo(score: 50);
        Api.Responder("GET casos", new ResultadoPaginado<CasoResumo>([caso], 1, 50, 1));

        var tela = RenderizarTela<PaginaCasos>();

        tela.WaitForAssertion(() => tela.FindAll("tbody tr").Count.ShouldBe(1));
        tela.Markup.ShouldContain(caso.IbanDestino!);
        tela.Markup.ShouldContain("1,000.00 EUR");
        tela.Find($"a[href='casos/{caso.Id}']").ShouldNotBeNull();
        Api.Pedidos.ShouldHaveSingleItem().Caminho.ShouldBe("casos?pagina=1&tamanho=50");
    }

    [Fact]
    public void CasoNovoPeloKafka_AFilaRecarregaSozinha()
    {
        var primeiro = Exemplos.Resumo(score: 40);
        var novo = Exemplos.Resumo(score: 60, iban: "MT84MALT011000012345MTLCAST001S");
        var fila = new List<CasoResumo> { primeiro };
        Api.ResponderCom("GET casos", () => new ResultadoPaginado<CasoResumo>([.. fila], 1, 50, fila.Count));
        var tela = RenderizarTela<PaginaCasos>();
        tela.WaitForAssertion(() => tela.FindAll("tbody tr").Count.ShouldBe(1));

        fila.Add(novo);
        Avisos.Publicar(new AvisoFila(novo.OperacaoId, novo.Id, novo.ScoreFinal, null, null, null));

        tela.WaitForAssertion(() => tela.FindAll("tbody tr").Count.ShouldBe(2));
        tela.Markup.ShouldContain(novo.IbanDestino!);
        Api.Pedidos.Count.ShouldBe(2);
    }

    [Fact]
    public async Task FechadaATela_OsAvisosNaoChegamMais()
    {
        Api.Responder("GET casos", new ResultadoPaginado<CasoResumo>([], 1, 50, 0));
        var tela = RenderizarTela<PaginaCasos>();
        tela.WaitForAssertion(() => Api.Pedidos.Count.ShouldBe(1));

        await DisposeComponentsAsync();
        Avisos.Publicar(new AvisoFila(Guid.CreateVersion7(), Guid.CreateVersion7(), 50, null, null, null));

        Api.Pedidos.Count.ShouldBe(1);
    }
}
