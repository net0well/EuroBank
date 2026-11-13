using System.Text.Json;
using EuroBank.Transferencias.Application.Commands;
using EuroBank.Transferencias.Application.Interfaces;
using EuroBank.Transferencias.Application.Replies;
using EuroBank.Transferencias.Application.TransferObjects;
using EuroBank.Transferencias.Domain.Enums;
using EuroBank.Transferencias.Domain.ValueObjects;
using EuroBank.Transferencias.Worker.Messaging;
using EuroBank.Transferencias.Worker.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EuroBank.Transferencias.Worker.Tests;

/// <summary>Confirmação depois do estorno (Divergente e reconciliação) e devolução pelo banco externo.</summary>
[Collection(nameof(ColecaoWorker))]
public sealed class ReconciliacaoEDevolucaoTestes(TransferenciasWorkerFixture worker)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public void Tabelas_TemAsRespostasNovas()
    {
        TiposRespostasSaga.Contas["ReconciliacaoRealizada"].ShouldBe(typeof(ReconciliacaoRealizada));
        TiposRespostasSaga.Contas["ReconciliacaoRecusada"].ShouldBe(typeof(ReconciliacaoRecusada));
        TiposRespostasSaga.Contas["DevolucaoCreditada"].ShouldBe(typeof(DevolucaoCreditada));
        TiposRespostasSaga.Contas["DevolucaoRecusada"].ShouldBe(typeof(DevolucaoRecusada));
        TiposRespostasSaga.BancoExterno["TransferenciaDevolvida"].ShouldBe(typeof(DevolucaoRecebida));
        TiposRespostasSaga.EventosContas["ContaEncerrada"].ShouldBe(typeof(EventoContaEncerrada));
    }

    [Fact]
    public async Task ConfirmacaoDepoisDoEstorno_FicaDivergente_EAReconciliacaoConclui()
    {
        var (id, conta) = await DivergenteAsync();

        await worker.EntregarAsync(new ReconciliarTransferencia(id.Valor));
        (await worker.ObterAsync(id)).Estado.ShouldBe(EstadoTransferencia.Reconciliando);
        await worker.EntregarAsync(new ReconciliacaoRealizada(id.Valor, conta, "102.50", "EUR"));

        var concluida = await worker.ObterAsync(id);
        concluida.Estado.ShouldBe(EstadoTransferencia.Concluida);
        concluida.Motivo.ShouldBe(MotivoTransferencia.Reconciliada);
        (await worker.ComandosAsync(id, conta)).ShouldBe(
        [
            "DebitarTransferenciaExterna",
            "LiquidarTransferencia",
            "EstornarTransferencia",
            "TransferenciaCancelada",
            "TransferenciaDivergente",
            "DebitarReconciliacao",
            "TransferenciaConcluida",
        ]);
    }

    [Fact]
    public async Task ReconciliacaoRecusada_VoltaParaDivergente()
    {
        var (id, conta) = await DivergenteAsync();
        await worker.EntregarAsync(new ReconciliarTransferencia(id.Valor));

        await worker.EntregarAsync(new ReconciliacaoRecusada(id.Valor, conta, "ContaEncerrada"));

        var divergente = await worker.ObterAsync(id);
        divergente.Estado.ShouldBe(EstadoTransferencia.Divergente);
        divergente.Motivo.ShouldBe(MotivoTransferencia.ConfirmadaAposEstorno);
    }

    [Fact]
    public async Task Devolucao_CreditaAOrigemETerminaDevolvida()
    {
        var (id, conta) = await ConcluidaAsync();

        await worker.EntregarAsync(new DevolucaoRecebida(id.Valor, 100m, "EUR", "Conta do beneficiário encerrada"));
        (await worker.ObterAsync(id)).Estado.ShouldBe(EstadoTransferencia.AguardandoCreditoDevolucao);
        await worker.EntregarAsync(new DevolucaoCreditada(id.Valor, conta, "100.00", "EUR"));

        var devolvida = await worker.ObterAsync(id);
        devolvida.Estado.ShouldBe(EstadoTransferencia.Devolvida);
        devolvida.Motivo.ShouldBe(MotivoTransferencia.DevolvidaPeloBanco);
        (await worker.ComandosAsync(id, conta)).ShouldBe(
        [
            "DebitarTransferenciaExterna",
            "LiquidarTransferencia",
            "LiberarTravaTransferencia",
            "TransferenciaConcluida",
            "CreditarDevolucao",
            "TransferenciaDevolvida",
        ]);

        await using var contexto = worker.CriarContexto();
        var evento = await contexto.Outbox.SingleAsync(m => m.IdAgregado == id.ToString() && m.TipoEvento == "TransferenciaDevolvida", Ct);
        evento.TipoAgregado.ShouldBe("transferencias.eventos");
        using var payload = JsonDocument.Parse(evento.Payload);
        payload.RootElement.GetProperty("valorDevolvido").GetString().ShouldBe("100.00");
        payload.RootElement.GetProperty("moeda").GetString().ShouldBe("EUR");
    }

    [Fact]
    public async Task DevolucaoRecusada_FicaDivergente()
    {
        var (id, conta) = await ConcluidaAsync();
        await worker.EntregarAsync(new DevolucaoRecebida(id.Valor, 100m, "EUR", "Conta do beneficiário encerrada"));

        await worker.EntregarAsync(new DevolucaoRecusada(id.Valor, conta, "ContaEncerrada"));

        var divergente = await worker.ObterAsync(id);
        divergente.Estado.ShouldBe(EstadoTransferencia.Divergente);
        divergente.Motivo.ShouldBe(MotivoTransferencia.DevolucaoNaoCreditada);
    }

    [Fact]
    public async Task ContaEncerrada_MarcaAProjecao_EORepetidoNaoMudaNada()
    {
        var contaId = Guid.CreateVersion7();
        await worker.EntregarAsync(new EventoContaAberta(
            contaId, $"PT50EURO{Random.Shared.NextInt64(1, 9_999_999_999_999):D13}", "Fábio", DateTimeOffset.UtcNow, "EUR"));

        await worker.EntregarAsync(new EventoContaEncerrada(contaId, DateTimeOffset.UtcNow));
        await worker.EntregarAsync(new EventoContaEncerrada(contaId, DateTimeOffset.UtcNow));
        await worker.EntregarAsync(new EventoContaEncerrada(Guid.CreateVersion7(), DateTimeOffset.UtcNow));

        await using var escopo = worker.Servicos.CreateAsyncScope();
        (await escopo.ServiceProvider.GetRequiredService<IContasConhecidas>().ObterPorIdAsync(contaId, Ct))
            .ShouldNotBeNull().Encerrada.ShouldBeTrue();
    }

    /// <summary>Débito, recusa do banco, estorno e, depois, a confirmação tardia.</summary>
    private async Task<(TransferenciaId Id, Guid Conta)> DivergenteAsync()
    {
        var id = await worker.CriarAsync();
        var conta = (await worker.ObterAsync(id)).ContaOrigemId;
        await worker.EntregarAsync(new DebitoRealizado(id.Valor, conta, 100m, 2.50m));
        await worker.EntregarAsync(new LiquidacaoRecusada(id.Valor, "Conta de destino inexistente"));
        await worker.EntregarAsync(new EstornoRealizado(id.Valor, conta, 102.50m));
        await worker.EntregarAsync(new LiquidacaoConfirmada(id.Valor, worker.Relogio.GetUtcNow()));

        var divergente = await worker.ObterAsync(id);
        divergente.Estado.ShouldBe(EstadoTransferencia.Divergente);
        divergente.Motivo.ShouldBe(MotivoTransferencia.ConfirmadaAposEstorno);
        return (id, conta);
    }

    private async Task<(TransferenciaId Id, Guid Conta)> ConcluidaAsync()
    {
        var id = await worker.CriarAsync("DE89370400440532013094");
        var conta = (await worker.ObterAsync(id)).ContaOrigemId;
        await worker.EntregarAsync(new DebitoRealizado(id.Valor, conta, 100m, 2.50m));
        await worker.EntregarAsync(new LiquidacaoConfirmada(id.Valor, worker.Relogio.GetUtcNow()));
        return (id, conta);
    }
}
