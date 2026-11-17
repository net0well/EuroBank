using System.Collections.Concurrent;
using EuroBank.Antifraude.Painel.Clients;
using EuroBank.Antifraude.Painel.Services;
using EuroBank.Antifraude.Painel.TransferObjects;
using EuroBank.E2E.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace EuroBank.E2E.Tests;

/// <summary>
/// O painel do analista no laboratório: o caso novo chega pelo Kafka (CasoAberto pelo Debezium) sem ninguém recarregar
/// nada, o analista vê a análise, assume e aprova pelo cliente do painel (com a API key do servidor), e a saga
/// reage. A tela reagindo ao aviso é coberta pelos testes de componente.
/// </summary>
public sealed class PainelAnalistaTestes(LaboratorioFixture lab)
{
    private const string BicMalta = "MALTMTMT";

    private static readonly TimeSpan Limite = TimeSpan.FromSeconds(60);

    private readonly CenarioSaga _cenario = new(lab);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task CasoNovoChegaAoPainel_AnalistaAprovaPeloPainel_ESagaConclui()
    {
        var avisos = new ConcurrentQueue<AvisoFila>();
        using var assinatura = lab.Painel.GetRequiredService<AvisosFila>().Assinar(avisos.Enqueue);
        var cliente = lab.Painel.GetRequiredService<ClienteAntifraude>();

        var conta = await _cenario.AbrirContaAsync(1_000m);
        var pedido = await _cenario.SolicitarParaIbanComSucessoAsync(conta, LaboratorioFixture.IbanPaisDeRisco, BicMalta, 200m);
        var emRevisao = await _cenario.EsperarEstadoAsync(pedido.Id, "EmAnaliseManual", Limite);
        var casoId = emRevisao.CasoRevisaoId!.Value;

        await Esperar.AteAsync(
            () => Task.FromResult(avisos.Any(aviso => aviso.CasoId == casoId)), Limite, "o CasoAberto chegar ao painel pelo Kafka");
        (await cliente.ListarCasosAsync(null, 1, 200, Ct)).Itens.ShouldContain(caso => caso.Id == casoId);

        var caso = await cliente.ObterCasoAsync(casoId, Ct);
        caso.Analise.OperacaoId.ShouldBe(pedido.Id);
        caso.Analise.Sinais.Where(sinal => sinal.Disparou && !sinal.Sombra).Select(sinal => sinal.Codigo)
            .ShouldBe(["BeneficiarioNovo", "PaisDeRisco"], ignoreOrder: true);
        caso.Analise.Features.ShouldNotBeNull().Count.ShouldBe(11);

        (await cliente.AssumirAsync(casoId, "Marta", Ct)).Estado.ShouldBe("EmAnalise");
        (await cliente.DecidirAsync(casoId, new DecisaoRequisicao("Marta", "Aprovar", "Cliente confirmou por telefone", false), Ct))
            .Estado.ShouldBe("Aprovado");

        var concluida = await _cenario.EsperarEstadoAsync(pedido.Id, "Concluida", Limite);
        concluida.PassouPor("Aprovada pelo analista").ShouldBeTrue();
        await Esperar.AteAsync(
            () => Task.FromResult(avisos.Any(aviso => aviso.OperacaoId == pedido.Id && aviso.Origem == "Analista" && aviso.Decisao == "Aprovada")),
            Limite,
            "a decisão do analista chegar ao painel");
    }
}
