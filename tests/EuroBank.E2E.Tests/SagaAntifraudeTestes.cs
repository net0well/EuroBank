using System.Net;
using EuroBank.E2E.Tests.Fixtures;
using static EuroBank.E2E.Tests.Fixtures.CenarioSaga;

namespace EuroBank.E2E.Tests;

/// <summary>
/// A saga com o antifraude de verdade: o worker chama o gRPC pela varredura, a decisão do analista volta pelo
/// Debezium (eurobank.antifraude.eventos) e o cancelamento do cliente fecha o caso. Revisão: Malta é país de risco
/// neste laboratório (30) e o beneficiário é novo (10), score 40.
/// </summary>
public sealed class SagaAntifraudeTestes(LaboratorioFixture lab)
{
    private const string BicMalta = "MALTMTMT";

    private static readonly TimeSpan Limite = TimeSpan.FromSeconds(60);

    private readonly CenarioSaga _cenario = new(lab);

    private readonly CenarioAntifraude _antifraude = new(lab);

    private async Task<(Guid Conta, TransferenciaE2E EmRevisao)> EmRevisaoAsync(decimal saldo = 1_000m, decimal valor = 200m)
    {
        var conta = await _cenario.AbrirContaAsync(saldo);
        var pedido = await _cenario.SolicitarParaIbanComSucessoAsync(conta, LaboratorioFixture.IbanPaisDeRisco, BicMalta, valor);
        return (conta, await _cenario.EsperarEstadoAsync(pedido.Id, "EmAnaliseManual", Limite));
    }

    [Fact]
    public async Task TransferenciaComum_AntifraudeAprova_ESagaConclui()
    {
        var conta = await _cenario.AbrirContaAsync(1_000m);
        var primeira = await _cenario.SolicitarComSucessoAsync(conta, "00", 100m);
        primeira.Estado.ShouldBe("AnalisandoRisco");
        await _cenario.EsperarEstadoAsync(primeira.Id, "Concluida", Limite);

        var segunda = await _cenario.SolicitarComSucessoAsync(conta, "00", 50m);
        var concluida = await _cenario.EsperarEstadoAsync(segunda.Id, "Concluida", Limite);

        concluida.PassouPor("Aprovada pelo antifraude").ShouldBeTrue();
        concluida.CasoRevisaoId.ShouldBeNull();
        var analise = await _antifraude.AnaliseAsync(concluida.AnaliseRiscoId!.Value);
        analise.GetProperty("decisao").GetString().ShouldBe("DECISAO_APROVADA");
        analise.GetProperty("operacaoId").GetString().ShouldBe(segunda.Id.ToString());
        (await _cenario.SaldoAsync(conta)).ShouldBe(1_000m - 102.50m - 52.50m);
    }

    [Fact]
    public async Task IbanNaListaDeBloqueio_RecusadaSemDebito()
    {
        var conta = await _cenario.AbrirContaAsync(500m);

        // DE89370400440532013066 entra bloqueado pela migration do antifraude.
        var pedido = await _cenario.SolicitarComSucessoAsync(conta, "66", 100m);
        var recusada = await _cenario.EsperarEstadoAsync(pedido.Id, "Recusada", Limite);

        recusada.Motivo.ShouldBe("RecusadaPeloAntifraude");
        recusada.PassouPor("Recusada pelas regras do antifraude").ShouldBeTrue();
        recusada.PassouPor("DebitarTransferenciaExterna").ShouldBeFalse();
        (await _antifraude.RegrasQuePontuaramAsync(recusada.AnaliseRiscoId!.Value)).ShouldContain("ListaBloqueio");
        (await _cenario.SaldoAsync(conta)).ShouldBe(500m);
    }

    [Fact]
    public async Task EmRevisao_AnalistaAprova_ESagaConclui_ComUmaAnaliseEUmCaso()
    {
        var (conta, emRevisao) = await EmRevisaoAsync();
        var casoId = emRevisao.CasoRevisaoId!.Value;

        var caso = await _antifraude.CasoAsync(casoId);
        caso.GetProperty("analise").GetProperty("operacaoId").GetGuid().ShouldBe(emRevisao.Id);
        caso.GetProperty("analise").GetProperty("scoreFinal").GetInt32().ShouldBe(40);
        (await _antifraude.RegrasQuePontuaramAsync(emRevisao.AnaliseRiscoId!.Value))
            .ShouldBe(["BeneficiarioNovo", "PaisDeRisco"], ignoreOrder: true);

        // A mesma operação avaliada de novo devolve a mesma análise e o mesmo caso.
        var repetida = await _antifraude.AvaliarDeNovoAsync(emRevisao.Id, conta, LaboratorioFixture.IbanPaisDeRisco, 200m);
        repetida.GetProperty("analiseId").GetString().ShouldBe(emRevisao.AnaliseRiscoId.ToString());
        repetida.GetProperty("casoId").GetString().ShouldBe(casoId.ToString());
        (await _antifraude.CasosPendentesDaOperacaoAsync(emRevisao.Id)).ShouldBe(1);

        await _antifraude.DecidirAsync(casoId, "Aprovar");

        var concluida = await _cenario.EsperarEstadoAsync(emRevisao.Id, "Concluida", Limite);
        concluida.PassouPor($"Aprovada pelo analista no caso {casoId}").ShouldBeTrue();
        (await _cenario.SaldoAsync(conta)).ShouldBe(1_000m - 202.50m);
    }

    [Fact]
    public async Task EmRevisao_AnalistaRecusa_RecusadaSemDebito()
    {
        var (conta, emRevisao) = await EmRevisaoAsync();

        await _antifraude.DecidirAsync(emRevisao.CasoRevisaoId!.Value, "Recusar");

        var recusada = await _cenario.EsperarEstadoAsync(emRevisao.Id, "Recusada", Limite);
        recusada.Motivo.ShouldBe("RecusadaPeloAntifraude");
        recusada.PassouPor("Recusada pelo analista").ShouldBeTrue();
        (await _cenario.SaldoAsync(conta)).ShouldBe(1_000m);
    }

    [Fact]
    public async Task EmRevisao_SemDecisaoNoPrazo_RevisaoExpirada()
    {
        var (conta, emRevisao) = await EmRevisaoAsync();

        // Os casos expiram em 20 s neste laboratório.
        var recusada = await _cenario.EsperarEstadoAsync(emRevisao.Id, "Recusada", Limite);

        recusada.Motivo.ShouldBe("RevisaoExpirada");
        (await _antifraude.CasoAsync(emRevisao.CasoRevisaoId!.Value)).GetProperty("estado").GetString().ShouldBe("Expirado");
        (await _cenario.SaldoAsync(conta)).ShouldBe(1_000m);
    }

    [Fact]
    public async Task CancelamentoNaRevisao_TerminaCancelada_EFechaOCaso()
    {
        var (conta, emRevisao) = await EmRevisaoAsync();
        var casoId = emRevisao.CasoRevisaoId!.Value;

        var cancelada = await LerAsync(await _cenario.CancelarAsync(emRevisao.Id), HttpStatusCode.Accepted);

        cancelada.Estado.ShouldBe("Cancelada");
        cancelada.Motivo.ShouldBe("CanceladaPeloCliente");
        await Esperar.AteAsync(
            async () => (await _antifraude.CasoAsync(casoId)).GetProperty("estado").GetString() == "CanceladoPeloCliente",
            Limite,
            $"o caso {casoId} não foi fechado pelo cancelamento");
        (await _cenario.SaldoAsync(conta)).ShouldBe(1_000m);
    }
}
