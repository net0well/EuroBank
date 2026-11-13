using System.Net;
using System.Net.Http.Json;
using EuroBank.E2E.Tests.Fixtures;

namespace EuroBank.E2E.Tests;

/// <summary>
/// O que o banco externo faz depois de a saga terminar: confirmar uma transferência já estornada (final 95, Divergente
/// e reconciliação) e devolver uma já concluída (final 94).
/// </summary>
public sealed class SagaExternaDivergenciaTestes(LaboratorioFixture lab)
{
    private static readonly TimeSpan Limite = TimeSpan.FromSeconds(90);

    private readonly CenarioSaga _cenario = new(lab);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Final95_ConfirmaDepoisDoEstorno_FicaDivergente_EAReconciliacaoDeixaAContaNegativa()
    {
        var conta = await _cenario.AbrirContaAsync(102.50m);
        var pedido = await _cenario.SolicitarComSucessoAsync(conta, "95", 100m);

        var divergente = await _cenario.EsperarEstadoAsync(pedido.Id, "Divergente", Limite);
        divergente.Motivo.ShouldBe("ConfirmadaAposEstorno");
        divergente.PassouPor("StatusLiquidacao recebido do banco externo: NaoLiquidada").ShouldBeTrue();
        divergente.PassouPor("EstornoRealizado recebido de Contas").ShouldBeTrue();
        (await _cenario.SaldoAsync(conta)).ShouldBe(102.50m);

        // O cliente usa o dinheiro estornado antes de alguém reconciliar.
        (await lab.Contas.PostAsJsonAsync($"/contas/{conta}/saques", new { valor = 52.50m }, Ct)).EnsureSuccessStatusCode();
        var reconciliacao = await lab.Transferencias.PostAsync($"/transferencias-externas/{pedido.Id}/reconciliacao", content: null, Ct);
        (await CenarioSaga.LerAsync(reconciliacao, HttpStatusCode.Accepted)).Estado.ShouldBe("Reconciliando");

        var reconciliada = await _cenario.EsperarEstadoAsync(pedido.Id, "Concluida", Limite);
        reconciliada.Motivo.ShouldBe("Reconciliada");
        (await _cenario.SaldoAsync(conta)).ShouldBe(-52.50m);
        var lancamentos = await _cenario.EsperarLancamentosAsync(conta, 5, Limite);
        lancamentos.Select(lancamento => lancamento.GetProperty("tipo").GetString())
            .ShouldBe(["Deposito", "TransferenciaExternaEnviada", "EstornoTransferencia", "Saque", "DebitoReconciliacao"]);
        lancamentos[^1].GetProperty("valor").GetDecimal().ShouldBe(102.50m);
        lancamentos[^1].GetProperty("saldoApos").GetDecimal().ShouldBe(-52.50m);

        // Com a conta negativa, a próxima transferência é recusada sem debitar nada.
        var seguinte = await _cenario.SolicitarComSucessoAsync(conta, "00", 10m);
        var recusada = await _cenario.EsperarEstadoAsync(seguinte.Id, "Recusada", Limite);
        recusada.Motivo.ShouldBe("SaldoNegativo");
        (await _cenario.SaldoAsync(conta)).ShouldBe(-52.50m);
    }

    [Fact]
    public async Task Final94_ConcluiEDepoisODinheiroVolta_SemATarifa()
    {
        var conta = await _cenario.AbrirContaAsync(1_000m);
        var pedido = await _cenario.SolicitarComSucessoAsync(conta, "94", 100m);

        var devolvida = await _cenario.EsperarEstadoAsync(pedido.Id, "Devolvida", Limite);

        devolvida.Motivo.ShouldBe("DevolvidaPeloBanco");
        devolvida.PassouPor("LiquidacaoConfirmada recebida do banco externo").ShouldBeTrue();
        devolvida.PassouPor("TransferenciaDevolvida recebida do banco externo").ShouldBeTrue();
        (await _cenario.SaldoAsync(conta)).ShouldBe(997.50m);
        var lancamentos = await _cenario.EsperarLancamentosAsync(conta, 3, Limite);
        lancamentos.Select(lancamento => lancamento.GetProperty("tipo").GetString())
            .ShouldBe(["Deposito", "TransferenciaExternaEnviada", "DevolucaoTransferenciaExterna"]);
        lancamentos[^1].GetProperty("valor").GetDecimal().ShouldBe(100m);
        lancamentos[^1].GetProperty("saldoApos").GetDecimal().ShouldBe(997.50m);
    }
}
