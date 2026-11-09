using System.Net;
using System.Text.Json;
using EuroBank.Contas.Api.Tests.Fixtures;
using EuroBank.Contas.Application.Commands;
using EuroBank.Contas.Application.Handlers;
using EuroBank.Contas.Application.Services;
using EuroBank.Contas.Domain.Entities;
using EuroBank.Contas.Domain.Enums;
using EuroBank.Contas.Domain.ValueObjects;
using EuroBank.Contas.Infrastructure.Outbox;
using EuroBank.Shared.Persistence.Outbox;
using EuroBank.Shared.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EuroBank.Contas.Api.Tests.Saga;

/// <summary>Comandos que a saga manda para Contas: efeito na conta e respostas gravadas na outbox.</summary>
public sealed class ComandosSagaTestes(PostgresFixture banco)
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<Conta> CriarContaAsync(decimal saldo)
    {
        await using var cenario = new CenarioPersistencia(banco);
        var conta = Conta.Abrir(await cenario.GeradorIban.GerarAsync(Ct), Titular.Criar("Saga"), Agora);
        if (saldo > 0)
        {
            conta.Depositar(Dinheiro.De(saldo), Agora);
        }

        cenario.Repositorio.Adicionar(conta);
        await cenario.UnidadeDeTrabalho.SalvarAsync(Ct);
        return conta;
    }

    private async Task DebitarAsync(Guid transferenciaId, Guid contaId, decimal valor, decimal tarifa = 2.50m)
    {
        await using var cenario = new CenarioPersistencia(banco);
        var manipulador = new DebitarTransferenciaExternaManipulador(
            Passo(cenario), new RespostasSagaOutbox(cenario.Fila, TimeProvider.System), TimeProvider.System);
        await manipulador.Handle(
            new DebitarTransferenciaExterna(transferenciaId, contaId, valor, tarifa, "DE89370400440532013000", "Bruno", "Aluguel"), Ct);
    }

    private static PassoDaSaga Passo(CenarioPersistencia cenario) =>
        new(cenario.Repositorio, cenario.UnidadeDeTrabalho, NullLogger<PassoDaSaga>.Instance);

    private async Task EstornarAsync(Guid transferenciaId, Guid contaId)
    {
        await using var cenario = new CenarioPersistencia(banco);
        await new EstornarTransferenciaManipulador(
                cenario.Repositorio, new RespostasSagaOutbox(cenario.Fila, TimeProvider.System), cenario.UnidadeDeTrabalho, TimeProvider.System)
            .Handle(new EstornarTransferencia(transferenciaId, contaId), Ct);
    }

    private async Task<List<MensagemOutbox>> MensagensAsync(params string[] idsAgregado)
    {
        await using var leitura = banco.CriarContexto();
        return await leitura.Outbox
            .Where(m => idsAgregado.Contains(m.IdAgregado))
            .OrderBy(m => m.Sequencia)
            .ToListAsync(Ct);
    }

    [Fact]
    public async Task Debitar_DebitaComTarifa_GravaTravaEventoEResposta()
    {
        var conta = await CriarContaAsync(1000m);
        var transferenciaId = Guid.CreateVersion7();

        await DebitarAsync(transferenciaId, conta.Id.Valor, 100m);

        await using var leitura = banco.CriarContexto();
        var gravada = await leitura.Contas.SingleAsync(c => c.Id == conta.Id, Ct);
        gravada.Saldo.ShouldBe(Dinheiro.De(897.50m));
        gravada.Transferencias.ShouldHaveSingleItem().Status.ShouldBe(StatusTransferenciaDaConta.Pendente);

        var mensagens = await MensagensAsync(conta.Id.ToString(), transferenciaId.ToString());
        var evento = mensagens.Single(m => m.TipoEvento == "TransferenciaExternaEnviada");
        evento.TipoAgregado.ShouldBe("contas.eventos");
        var resposta = mensagens.Single(m => m.TipoEvento == "DebitoRealizado");
        resposta.TipoAgregado.ShouldBe("contas.respostas");
        resposta.IdAgregado.ShouldBe(transferenciaId.ToString());
        resposta.Sequencia.ShouldBeGreaterThan(evento.Sequencia);
        using var payload = JsonDocument.Parse(resposta.Payload);
        payload.RootElement.GetProperty("tarifa").GetString().ShouldBe("2.50");
    }

    [Fact]
    public async Task DebitoReenviado_NaoDebitaDeNovo_MasRespondeDeNovo()
    {
        var conta = await CriarContaAsync(1000m);
        var transferenciaId = Guid.CreateVersion7();

        await DebitarAsync(transferenciaId, conta.Id.Valor, 100m);
        await DebitarAsync(transferenciaId, conta.Id.Valor, 100m);

        await using var leitura = banco.CriarContexto();
        (await leitura.Contas.SingleAsync(c => c.Id == conta.Id, Ct)).Saldo.ShouldBe(Dinheiro.De(897.50m));
        var mensagens = await MensagensAsync(conta.Id.ToString(), transferenciaId.ToString());
        mensagens.Count(m => m.TipoEvento == "TransferenciaExternaEnviada").ShouldBe(1);
        mensagens.Count(m => m.TipoEvento == "DebitoRealizado").ShouldBe(2);
    }

    [Fact]
    public async Task DebitoSemSaldo_RespondeRecusaSemAlterarAConta()
    {
        var conta = await CriarContaAsync(50m);
        var transferenciaId = Guid.CreateVersion7();

        await DebitarAsync(transferenciaId, conta.Id.Valor, 100m);

        var resposta = (await MensagensAsync(transferenciaId.ToString())).ShouldHaveSingleItem();
        resposta.TipoEvento.ShouldBe("DebitoRecusado");
        using var payload = JsonDocument.Parse(resposta.Payload);
        payload.RootElement.GetProperty("motivo").GetString().ShouldBe("SaldoInsuficiente");
        (await MensagensAsync(conta.Id.ToString())).ShouldAllBe(m => m.TipoEvento != "TransferenciaExternaEnviada");
    }

    [Fact]
    public async Task DebitoEmContaInexistente_RespondeContaInexistente()
    {
        var transferenciaId = Guid.CreateVersion7();

        await DebitarAsync(transferenciaId, Guid.CreateVersion7(), 10m);

        using var payload = JsonDocument.Parse((await MensagensAsync(transferenciaId.ToString())).ShouldHaveSingleItem().Payload);
        payload.RootElement.GetProperty("motivo").GetString().ShouldBe("ContaInexistente");
    }

    [Fact]
    public async Task Estornar_DevolveValorMaisTarifa_ERespondeEstornoRealizado()
    {
        var conta = await CriarContaAsync(1000m);
        var transferenciaId = Guid.CreateVersion7();
        await DebitarAsync(transferenciaId, conta.Id.Valor, 100m);

        await EstornarAsync(transferenciaId, conta.Id.Valor);
        await EstornarAsync(transferenciaId, conta.Id.Valor);

        await using var leitura = banco.CriarContexto();
        (await leitura.Contas.SingleAsync(c => c.Id == conta.Id, Ct)).Saldo.ShouldBe(Dinheiro.De(1000m));
        var mensagens = await MensagensAsync(conta.Id.ToString(), transferenciaId.ToString());
        mensagens.Count(m => m.TipoEvento == "EstornoTransferencia").ShouldBe(1);
        using var payload = JsonDocument.Parse(mensagens.First(m => m.TipoEvento == "EstornoRealizado").Payload);
        payload.RootElement.GetProperty("valorDevolvido").GetString().ShouldBe("102.50");
    }

    [Fact]
    public async Task EncerrarPelaApi_ComTransferenciaEmAndamento_Responde422()
    {
        using var http = banco.Api.CriarClienteAutenticado();
        var cliente = new ClienteContas(http);
        var conta = await cliente.AbrirAsync(depositoInicial: 102.50m);
        await using (var cenario = new CenarioPersistencia(banco.ConnectionStringApi))
        {
            var manipulador = new DebitarTransferenciaExternaManipulador(
            Passo(cenario), new RespostasSagaOutbox(cenario.Fila, TimeProvider.System), TimeProvider.System);
            await manipulador.Handle(
                new DebitarTransferenciaExterna(Guid.CreateVersion7(), conta.Id, 100m, 2.50m, "DE89370400440532013000", "Bruno", null), Ct);
        }

        await ClienteContas.DeveSerProblemaAsync(
            await cliente.ComandoAsync(conta.Id, "encerramento"), (HttpStatusCode)422, "conta_com_transferencia_em_andamento");
    }
}
