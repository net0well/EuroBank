using System.Text.Json;
using EuroBank.Contas.Api.Tests.Fixtures;
using EuroBank.Contas.Application.Commands;
using EuroBank.Contas.Application.Handlers;
using EuroBank.Contas.Application.Services;
using EuroBank.Contas.Domain.Entities;
using EuroBank.Contas.Domain.ValueObjects;
using EuroBank.Contas.Infrastructure.Outbox;
using EuroBank.Shared.Enums;
using EuroBank.Shared.Persistence.Outbox;
using EuroBank.Shared.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EuroBank.Contas.Api.Tests.Saga;

/// <summary>Reconciliação (saldo negativo) e devolução pelo banco externo, contra o Postgres real.</summary>
public sealed class ComandosReconciliacaoTestes(PostgresFixture banco)
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 10, 12, 0, 0, TimeSpan.Zero);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static PassoDaSaga Passo(CenarioPersistencia cenario) =>
        new(cenario.Repositorio, cenario.UnidadeDeTrabalho, NullLogger<PassoDaSaga>.Instance);

    /// <summary>Conta com uma transferência de 100 + 2,50 debitada e, conforme o caso, estornada ou concluída.</summary>
    private async Task<(Conta Conta, Guid TransferenciaId)> CriarAsync(decimal saldoInicial, bool estornar, bool encerrar = false)
    {
        await using var cenario = new CenarioPersistencia(banco);
        var conta = Conta.Abrir(await cenario.GeradorIban.GerarAsync(Ct), Titular.Criar("Reconciliação"), Agora);
        conta.Depositar(Dinheiro.De(saldoInicial), Agora);
        var transferenciaId = Guid.CreateVersion7();
        conta.DebitarTransferenciaExterna(transferenciaId, Dinheiro.De(100m), Dinheiro.De(2.50m), "DE89370400440532013095", "Bruno", null, Agora);
        if (estornar)
        {
            conta.EstornarTransferencia(transferenciaId, Agora);
            conta.Sacar(Dinheiro.De(saldoInicial - 50m), Agora);
        }
        else
        {
            conta.LiberarTrava(transferenciaId);
            if (encerrar)
            {
                if (saldoInicial > 102.50m)
                {
                    conta.Sacar(Dinheiro.De(saldoInicial - 102.50m), Agora);
                }

                conta.Encerrar(Agora);
            }
        }

        cenario.Repositorio.Adicionar(conta);
        await cenario.UnidadeDeTrabalho.SalvarAsync(Ct);
        return (conta, transferenciaId);
    }

    private async Task<List<MensagemOutbox>> MensagensAsync(params string[] idsAgregado)
    {
        await using var leitura = banco.CriarContexto();
        return await leitura.Outbox.Where(m => idsAgregado.Contains(m.IdAgregado)).OrderBy(m => m.Sequencia).ToListAsync(Ct);
    }

    [Fact]
    public async Task Reconciliacao_DeixaAContaNegativa_PersisteERespondeASaga()
    {
        var (conta, transferenciaId) = await CriarAsync(saldoInicial: 200m, estornar: true);

        await using (var cenario = new CenarioPersistencia(banco))
        {
            await new DebitarReconciliacaoManipulador(Passo(cenario), new RespostasSagaOutbox(cenario.Fila, TimeProvider.System), TimeProvider.System)
                .Handle(new DebitarReconciliacao(transferenciaId, conta.Id.Valor), Ct);
        }

        await using var leitura = banco.CriarContexto();
        (await leitura.Contas.SingleAsync(c => c.Id == conta.Id, Ct)).Saldo.ShouldBe(Saldo.De(-52.50m, Moeda.EUR));
        var mensagens = await MensagensAsync(conta.Id.ToString(), transferenciaId.ToString());
        var evento = mensagens.Single(m => m.TipoEvento == "DebitoReconciliacao");
        using (var payload = JsonDocument.Parse(evento.Payload))
        {
            payload.RootElement.GetProperty("saldoApos").GetString().ShouldBe("-52.50");
        }

        mensagens.ShouldContain(m => m.TipoEvento == "ReconciliacaoRealizada");
    }

    [Fact]
    public async Task ComSaldoNegativo_ODebitoDeTransferenciaERecusadoComSaldoNegativo()
    {
        var (conta, transferenciaId) = await CriarAsync(saldoInicial: 200m, estornar: true);
        await using (var cenario = new CenarioPersistencia(banco))
        {
            await new DebitarReconciliacaoManipulador(Passo(cenario), new RespostasSagaOutbox(cenario.Fila, TimeProvider.System), TimeProvider.System)
                .Handle(new DebitarReconciliacao(transferenciaId, conta.Id.Valor), Ct);
        }

        var outra = Guid.CreateVersion7();
        await using (var cenario = new CenarioPersistencia(banco))
        {
            await new DebitarTransferenciaExternaManipulador(Passo(cenario), new RespostasSagaOutbox(cenario.Fila, TimeProvider.System), TimeProvider.System)
                .Handle(new DebitarTransferenciaExterna(outra, conta.Id.Valor, 1m, 2.50m, "DE89370400440532013000", "Bruno", null), Ct);
        }

        using var resposta = JsonDocument.Parse((await MensagensAsync(outra.ToString())).ShouldHaveSingleItem().Payload);
        resposta.RootElement.GetProperty("motivo").GetString().ShouldBe("SaldoNegativo");
    }

    [Fact]
    public async Task Devolucao_CreditaOValorSemTarifa_ERespondeDevolucaoCreditada()
    {
        var (conta, transferenciaId) = await CriarAsync(saldoInicial: 1_000m, estornar: false);

        await using (var cenario = new CenarioPersistencia(banco))
        {
            await new CreditarDevolucaoManipulador(Passo(cenario), new RespostasSagaOutbox(cenario.Fila, TimeProvider.System), TimeProvider.System)
                .Handle(new CreditarDevolucao(transferenciaId, conta.Id.Valor), Ct);
        }

        await using var leitura = banco.CriarContexto();
        (await leitura.Contas.SingleAsync(c => c.Id == conta.Id, Ct)).Saldo.ShouldBe(Saldo.De(997.50m, Moeda.EUR));
        var mensagens = await MensagensAsync(conta.Id.ToString(), transferenciaId.ToString());
        mensagens.ShouldContain(m => m.TipoEvento == "DevolucaoTransferenciaExterna");
        mensagens.ShouldContain(m => m.TipoEvento == "DevolucaoCreditada");
    }

    [Fact]
    public async Task DevolucaoEmContaEncerrada_RespondeDevolucaoRecusada()
    {
        var (conta, transferenciaId) = await CriarAsync(saldoInicial: 102.50m, estornar: false, encerrar: true);

        await using (var cenario = new CenarioPersistencia(banco))
        {
            await new CreditarDevolucaoManipulador(Passo(cenario), new RespostasSagaOutbox(cenario.Fila, TimeProvider.System), TimeProvider.System)
                .Handle(new CreditarDevolucao(transferenciaId, conta.Id.Valor), Ct);
        }

        var resposta = (await MensagensAsync(transferenciaId.ToString())).ShouldHaveSingleItem();
        resposta.TipoEvento.ShouldBe("DevolucaoRecusada");
        using var payload = JsonDocument.Parse(resposta.Payload);
        payload.RootElement.GetProperty("motivo").GetString().ShouldBe("ContaEncerrada");
    }
}
