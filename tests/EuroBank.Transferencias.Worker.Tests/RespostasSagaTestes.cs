using EuroBank.Shared.Exceptions;
using EuroBank.Transferencias.Application.Replies;
using EuroBank.Transferencias.Domain.Enums;
using EuroBank.Transferencias.Worker.Messaging;
using EuroBank.Transferencias.Worker.Tests.Fixtures;

namespace EuroBank.Transferencias.Worker.Tests;

[Collection(nameof(ColecaoWorker))]
public sealed class RespostasSagaTestes(TransferenciasWorkerFixture worker)
{
    [Theory]
    [InlineData("DebitoRealizado", typeof(DebitoRealizado))]
    [InlineData("DebitoRecusado", typeof(DebitoRecusado))]
    [InlineData("EstornoRealizado", typeof(EstornoRealizado))]
    public void TabelaDeContas_TemAsTresRespostas(string tipo, Type esperado)
    {
        TiposRespostasSaga.Contas[tipo].ShouldBe(esperado);
    }

    [Theory]
    [InlineData("LiquidacaoConfirmada", typeof(LiquidacaoConfirmada))]
    [InlineData("LiquidacaoRecusada", typeof(LiquidacaoRecusada))]
    [InlineData("StatusLiquidacao", typeof(StatusLiquidacao))]
    public void TabelaDoBancoExterno_TemAsTresRespostas(string tipo, Type esperado)
    {
        TiposRespostasSaga.BancoExterno[tipo].ShouldBe(esperado);
    }

    [Fact]
    public async Task CaminhoFeliz_DebitoELiquidacao_ConcluemELiberamATrava()
    {
        var id = await worker.CriarAsync();
        var conta = (await worker.ObterAsync(id)).ContaOrigemId;

        await worker.EntregarAsync(new DebitoRealizado(id.Valor, conta, 100m, 2.50m));
        (await worker.ObterAsync(id)).Estado.ShouldBe(EstadoTransferencia.AguardandoLiquidacao);

        await worker.EntregarAsync(new LiquidacaoConfirmada(id.Valor, worker.Relogio.GetUtcNow()));
        await worker.EntregarAsync(new LiquidacaoConfirmada(id.Valor, worker.Relogio.GetUtcNow()));

        var concluida = await worker.ObterAsync(id);
        concluida.Estado.ShouldBe(EstadoTransferencia.Concluida);
        concluida.Historico.MaxBy(passo => passo.Ordem)!.Descricao.ShouldContain("ignorado");
        (await worker.ComandosAsync(id, conta)).ShouldBe(
            ["DebitarTransferenciaExterna", "LiquidarTransferencia", "LiberarTravaTransferencia", "TransferenciaConcluida"]);
    }

    [Fact]
    public async Task LiquidacaoRecusada_EstornaETerminaCancelada()
    {
        var id = await worker.CriarAsync();
        var conta = (await worker.ObterAsync(id)).ContaOrigemId;
        await worker.EntregarAsync(new DebitoRealizado(id.Valor, conta, 100m, 2.50m));

        await worker.EntregarAsync(new LiquidacaoRecusada(id.Valor, "Conta de destino inexistente"));
        await worker.EntregarAsync(new EstornoRealizado(id.Valor, conta, 102.50m));

        var cancelada = await worker.ObterAsync(id);
        cancelada.Estado.ShouldBe(EstadoTransferencia.Cancelada);
        cancelada.Motivo.ShouldBe(MotivoTransferencia.RecusadaPeloBanco);
        (await worker.ComandosAsync(id, conta)).ShouldBe(
            ["DebitarTransferenciaExterna", "LiquidarTransferencia", "EstornarTransferencia", "TransferenciaCancelada"]);
    }

    [Theory]
    [InlineData("SaldoInsuficiente", MotivoTransferencia.SaldoInsuficiente)]
    [InlineData("ContaInexistente", MotivoTransferencia.ContaInexistente)]
    [InlineData("MotivoNovoDeContas", MotivoTransferencia.RecusadaPorContas)]
    public async Task DebitoRecusado_TerminaRecusadaComOMotivo(string motivo, MotivoTransferencia esperado)
    {
        var id = await worker.CriarAsync();

        await worker.EntregarAsync(new DebitoRecusado(id.Valor, Guid.CreateVersion7(), motivo));

        var recusada = await worker.ObterAsync(id);
        recusada.Estado.ShouldBe(EstadoTransferencia.Recusada);
        recusada.Motivo.ShouldBe(esperado);
    }

    [Fact]
    public async Task StatusDesconhecido_EMensagemInvalida()
    {
        var id = await worker.CriarAsync();

        await Should.ThrowAsync<ExcecaoMensagemInvalida>(() => worker.EntregarAsync(new StatusLiquidacao(id.Valor, "Talvez")));
    }

    [Fact]
    public async Task RespostaDeTransferenciaDesconhecida_EIgnorada()
    {
        await worker.EntregarAsync(new LiquidacaoConfirmada(Guid.CreateVersion7(), worker.Relogio.GetUtcNow()));
    }
}
