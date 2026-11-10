using EuroBank.Shared.Enums;
using EuroBank.Transferencias.Application.Interfaces;
using EuroBank.Transferencias.Application.Replies;
using EuroBank.Transferencias.Application.TransferObjects;
using EuroBank.Transferencias.Domain.Enums;
using EuroBank.Transferencias.Worker.Messaging;
using EuroBank.Transferencias.Worker.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace EuroBank.Transferencias.Worker.Tests;

[Collection(nameof(ColecaoWorker))]
public sealed class RespostasCambioTestes(TransferenciasWorkerFixture worker)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private CotacaoEmitida CotacaoDeCem(Guid transferenciaId) => new(
        transferenciaId, Guid.CreateVersion7(), "EUR", "USD", 1.0692m, 93.53m, 100m, worker.Relogio.GetUtcNow().AddMinutes(10));

    [Theory]
    [InlineData("CotacaoEmitida", typeof(CotacaoEmitida))]
    [InlineData("CotacaoIndisponivel", typeof(CotacaoIndisponivel))]
    public void TabelaDoCambio_TemAsDuasRespostas(string tipo, Type esperado)
    {
        TiposRespostasSaga.Cambio[tipo].ShouldBe(esperado);
    }

    [Fact]
    public void TabelaDeContas_TemAsRespostasDoCredito()
    {
        TiposRespostasSaga.Contas["CreditoRealizado"].ShouldBe(typeof(CreditoRealizado));
        TiposRespostasSaga.Contas["CreditoRecusado"].ShouldBe(typeof(CreditoRecusado));
        TiposRespostasSaga.EventosContas["ContaAberta"].ShouldBe(typeof(EventoContaAberta));
    }

    [Fact]
    public async Task ContaAberta_EntraNaProjecao_EORepetidoNaoMudaNada()
    {
        var contaId = Guid.CreateVersion7();
        var evento = new EventoContaAberta(contaId, $"PT50EURO{Random.Shared.NextInt64(1, 9_999_999_999_999):D13}", "Elias", DateTimeOffset.UtcNow, "USD");

        await worker.EntregarAsync(evento);
        await worker.EntregarAsync(evento);

        await using var escopo = worker.Servicos.CreateAsyncScope();
        var conta = await escopo.ServiceProvider.GetRequiredService<IContasConhecidas>().ObterPorIdAsync(contaId, Ct);
        conta.ShouldNotBeNull().Moeda.ShouldBe(Moeda.USD);
        (await escopo.ServiceProvider.GetRequiredService<IContasConhecidas>().ObterPorIbanAsync(evento.Iban, Ct))!.Id.ShouldBe(contaId);
    }

    [Fact]
    public async Task CambioInterno_CotacaoDebitoECredito_Concluem()
    {
        var (id, origem, destino) = await worker.CriarCambioAsync();

        await worker.EntregarAsync(CotacaoDeCem(id.Valor));
        (await worker.ObterAsync(id)).Estado.ShouldBe(EstadoTransferencia.AguardandoDebito);
        await worker.EntregarAsync(new DebitoRealizado(id.Valor, origem.Id, 93.53m, 2.50m));
        (await worker.ObterAsync(id)).Estado.ShouldBe(EstadoTransferencia.AguardandoCredito);
        await worker.EntregarAsync(new CreditoRealizado(id.Valor, destino.Id, 100m, "USD"));

        (await worker.ObterAsync(id)).Estado.ShouldBe(EstadoTransferencia.Concluida);
        (await worker.ComandosPorChaveAsync(id.Valor, origem.Id, destino.Id)).ShouldBe(
        [
            "CotarCambio",
            "DebitarTransferenciaCambio",
            "CreditarTransferenciaCambio",
            "LiberarTravaTransferencia",
            "TransferenciaConcluida",
        ]);
    }

    [Fact]
    public async Task CambioInterno_DestinoEncerrado_EstornaAOrigem()
    {
        var (id, origem, destino) = await worker.CriarCambioAsync();
        await worker.EntregarAsync(CotacaoDeCem(id.Valor));
        await worker.EntregarAsync(new DebitoRealizado(id.Valor, origem.Id, 93.53m, 2.50m));

        await worker.EntregarAsync(new CreditoRecusado(id.Valor, destino.Id, "ContaEncerrada"));
        await worker.EntregarAsync(new EstornoRealizado(id.Valor, origem.Id, 96.03m));

        var cancelada = await worker.ObterAsync(id);
        cancelada.Estado.ShouldBe(EstadoTransferencia.Cancelada);
        cancelada.Motivo.ShouldBe(MotivoTransferencia.DestinoEncerrado);
    }

    [Fact]
    public async Task CotacaoIndisponivel_RecusaSemDebito()
    {
        var (id, origem, _) = await worker.CriarCambioAsync();

        await worker.EntregarAsync(new CotacaoIndisponivel(id.Valor, "EUR", "USD", "par indisponível"));

        var recusada = await worker.ObterAsync(id);
        recusada.Estado.ShouldBe(EstadoTransferencia.Recusada);
        recusada.Motivo.ShouldBe(MotivoTransferencia.CambioIndisponivel);
        (await worker.ComandosPorChaveAsync(origem.Id)).ShouldBeEmpty();
    }
}
