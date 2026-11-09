using System.Net;
using System.Net.Http.Json;
using EuroBank.E2E.Tests.Fixtures;
using static EuroBank.E2E.Tests.Fixtures.CenarioSaga;

namespace EuroBank.E2E.Tests;

/// <summary>Agendamento, cancelamento, Idempotency-Key e a trava semântica na conta.</summary>
public sealed class SagaExternaCicloDeVidaTestes(LaboratorioFixture lab)
{
    private static readonly TimeSpan Limite = TimeSpan.FromSeconds(60);

    private readonly CenarioSaga _cenario = new(lab);

    [Fact]
    public async Task Agendada_ComecaNaHora_EOSaldoEConferidoNaExecucao()
    {
        var conta = await _cenario.AbrirContaAsync(0m);

        var agendada = await _cenario.SolicitarComSucessoAsync(conta, "00", 100m, executarEm: DateTimeOffset.UtcNow.AddSeconds(4));
        agendada.Estado.ShouldBe("Agendada");

        // O saldo só precisa existir quando a saga começar.
        (await lab.Contas.PostAsJsonAsync($"/contas/{conta}/depositos", new { valor = 200m }, TestContext.Current.CancellationToken))
            .EnsureSuccessStatusCode();

        await _cenario.EsperarEstadoAsync(agendada.Id, "Concluida", Limite);
        (await _cenario.SaldoAsync(conta)).ShouldBe(97.50m);
    }

    [Fact]
    public async Task CancelarAgendada_TerminaCanceladaSemTocarNaConta()
    {
        var conta = await _cenario.AbrirContaAsync(1_000m);
        var agendada = await _cenario.SolicitarComSucessoAsync(conta, "00", 100m, executarEm: DateTimeOffset.UtcNow.AddDays(1));

        var cancelada = await LerAsync(await _cenario.CancelarAsync(agendada.Id), HttpStatusCode.Accepted);

        cancelada.Estado.ShouldBe("Cancelada");
        cancelada.Motivo.ShouldBe("CanceladaPeloCliente");
        (await _cenario.SaldoAsync(conta)).ShouldBe(1_000m);
    }

    [Fact]
    public async Task CancelarComODebitoEmAndamento_EstornaETerminaCancelada()
    {
        var conta = await _cenario.AbrirContaAsync(1_000m);
        var pedido = await _cenario.SolicitarComSucessoAsync(conta, "00", 100m);

        // O pedido acabou de sair: o débito ainda está a caminho de Contas (Debezium, Kafka, worker).
        var anotado = await LerAsync(await _cenario.CancelarAsync(pedido.Id), HttpStatusCode.Accepted);
        anotado.CancelamentoSolicitado.ShouldBeTrue();

        var cancelada = await _cenario.EsperarEstadoAsync(pedido.Id, "Cancelada", Limite);
        cancelada.Motivo.ShouldBe("CanceladaPeloCliente");
        (await _cenario.SaldoAsync(conta)).ShouldBe(1_000m);
    }

    [Fact]
    public async Task CancelarDepoisDeAPedirALiquidacao_Responde409()
    {
        var conta = await _cenario.AbrirContaAsync(1_000m);
        var pedido = await _cenario.SolicitarComSucessoAsync(conta, "98", 100m);
        await _cenario.EsperarEstadoAsync(pedido.Id, "AguardandoLiquidacao", Limite);

        await DeveSerProblemaAsync(await _cenario.CancelarAsync(pedido.Id), HttpStatusCode.Conflict, "cancelamento_nao_permitido");
    }

    [Fact]
    public async Task MesmaIdempotencyKey_UmaTransferenciaSo_ComOutroCorpo422()
    {
        var conta = await _cenario.AbrirContaAsync(1_000m);
        var chave = Guid.CreateVersion7().ToString();

        var primeira = await LerAsync(await _cenario.SolicitarAsync(conta, "00", 100m, idempotencyKey: chave), HttpStatusCode.Accepted);
        var repetida = await LerAsync(await _cenario.SolicitarAsync(conta, "00", 100m, idempotencyKey: chave), HttpStatusCode.Accepted);

        repetida.Id.ShouldBe(primeira.Id);
        await DeveSerProblemaAsync(
            await _cenario.SolicitarAsync(conta, "00", 300m, idempotencyKey: chave),
            HttpStatusCode.UnprocessableEntity,
            "idempotency_key_reutilizada");
        await _cenario.EsperarEstadoAsync(primeira.Id, "Concluida", Limite);
        (await _cenario.SaldoAsync(conta)).ShouldBe(897.50m);
    }

    [Fact]
    public async Task ContaComTransferenciaEmAndamento_NaoPodeSerEncerrada()
    {
        var conta = await _cenario.AbrirContaAsync(102.50m);
        var pedido = await _cenario.SolicitarComSucessoAsync(conta, "98", 100m);
        await _cenario.EsperarEstadoAsync(pedido.Id, "AguardandoLiquidacao", Limite);
        (await _cenario.SaldoAsync(conta)).ShouldBe(0m);

        await DeveSerProblemaAsync(
            await _cenario.ComandoContaAsync(conta, "encerramento"),
            HttpStatusCode.UnprocessableEntity,
            "conta_com_transferencia_em_andamento");
    }
}
