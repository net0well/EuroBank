using EuroBank.E2E.Tests.Fixtures;

namespace EuroBank.E2E.Tests;

/// <summary>
/// A saga externa inteira com Debezium de verdade: Transferencias, outbox, Kafka, Contas.Worker, banco externo
/// simulado e volta. Cada final de IBAN leva a saga por um caminho diferente.
/// </summary>
public sealed class SagaExternaCaminhosTestes(LaboratorioFixture lab)
{
    private static readonly TimeSpan Limite = TimeSpan.FromSeconds(60);

    private readonly CenarioSaga _cenario = new(lab);

    [Fact]
    public async Task IbanComum_ConcluiEmAte10Segundos_DebitaValorETarifa_EOExtratoMostraATarifa()
    {
        var conta = await _cenario.AbrirContaAsync(1_000m);

        var pedido = await _cenario.SolicitarComSucessoAsync(conta, "00", 100m);
        await _cenario.EsperarEstadoAsync(pedido.Id, "Concluida", TimeSpan.FromSeconds(10));

        (await _cenario.SaldoAsync(conta)).ShouldBe(897.50m);
        var lancamentos = await _cenario.EsperarLancamentosAsync(conta, 2, Limite);
        var transferencia = lancamentos[^1];
        transferencia.GetProperty("tipo").GetString().ShouldBe("TransferenciaExternaEnviada");
        transferencia.GetProperty("valor").GetDecimal().ShouldBe(100m);
        transferencia.GetProperty("tarifa").GetDecimal().ShouldBe(2.50m);
        transferencia.GetProperty("saldoApos").GetDecimal().ShouldBe(897.50m);
    }

    [Fact]
    public async Task Final99_BancoRecusa_EstornaETerminaCancelada_ComDebitoEEstornoNoExtrato()
    {
        var conta = await _cenario.AbrirContaAsync(1_000m);

        var pedido = await _cenario.SolicitarComSucessoAsync(conta, "99", 100m);
        var cancelada = await _cenario.EsperarEstadoAsync(pedido.Id, "Cancelada", Limite);

        cancelada.Motivo.ShouldBe("RecusadaPeloBanco");
        (await _cenario.SaldoAsync(conta)).ShouldBe(1_000m);
        var lancamentos = await _cenario.EsperarLancamentosAsync(conta, 3, Limite);
        lancamentos.Select(lancamento => lancamento.GetProperty("tipo").GetString())
            .ShouldBe(["Deposito", "TransferenciaExternaEnviada", "EstornoTransferencia"]);
        lancamentos[^1].GetProperty("valor").GetDecimal().ShouldBe(102.50m);
    }

    [Fact]
    public async Task Final98_BancoNuncaResponde_ConsultaEDepoisEstorna()
    {
        var conta = await _cenario.AbrirContaAsync(1_000m);

        var pedido = await _cenario.SolicitarComSucessoAsync(conta, "98", 100m);
        var cancelada = await _cenario.EsperarEstadoAsync(pedido.Id, "Cancelada", Limite);

        cancelada.Motivo.ShouldBe("SemRespostaDoBanco");
        cancelada.PassouPor("LiquidarTransferencia reenviado (tentativa 4)").ShouldBeTrue();
        cancelada.PassouPor("ConsultarLiquidacao reenviado (tentativa 4)").ShouldBeTrue();
        (await _cenario.SaldoAsync(conta)).ShouldBe(1_000m);
    }

    [Fact]
    public async Task Final97_RespostaAtrasada_ReenviaIgnoraADuplicadaEDebitaUmaVezSo()
    {
        var conta = await _cenario.AbrirContaAsync(1_000m);

        var pedido = await _cenario.SolicitarComSucessoAsync(conta, "97", 100m);
        var concluida = await _cenario.EsperarEstadoAsync(pedido.Id, "Concluida", Limite);

        concluida.PassouPor("LiquidarTransferencia reenviado (tentativa 2)").ShouldBeTrue();
        await Esperar.AteAsync(
            async () => (await _cenario.ObterAsync(pedido.Id)).PassouPor("LiquidacaoConfirmada ignorado"),
            Limite,
            "a confirmação duplicada do banco não chegou à saga");
        (await _cenario.ObterAsync(pedido.Id)).Estado.ShouldBe("Concluida");
        (await _cenario.SaldoAsync(conta)).ShouldBe(897.50m);
        var lancamentos = await _cenario.EsperarLancamentosAsync(conta, 2, Limite);
        lancamentos.Count(lancamento => lancamento.GetProperty("tipo").GetString() == "TransferenciaExternaEnviada").ShouldBe(1);
    }

    [Fact]
    public async Task Final96_RespostaPerdida_AConsultaDizLiquidadaEConcluiSemEstorno()
    {
        var conta = await _cenario.AbrirContaAsync(1_000m);

        var pedido = await _cenario.SolicitarComSucessoAsync(conta, "96", 100m);
        var concluida = await _cenario.EsperarEstadoAsync(pedido.Id, "Concluida", Limite);

        concluida.PassouPor("StatusLiquidacao recebido do banco externo: Liquidada").ShouldBeTrue();
        concluida.PassouPor("EstornarTransferencia").ShouldBeFalse();
        (await _cenario.SaldoAsync(conta)).ShouldBe(897.50m);
    }
}
