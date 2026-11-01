using System.Diagnostics;
using System.Text.Json;
using EuroBank.Contas.Api.Tests.Fixtures;
using EuroBank.Contas.Domain.Entities;
using EuroBank.Contas.Domain.Services;
using EuroBank.Contas.Domain.ValueObjects;
using EuroBank.Shared.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace EuroBank.Contas.Api.Tests.Persistence;

public sealed class OutboxTestes(PostgresFixture banco)
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 31, 12, 0, 0, TimeSpan.Zero);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task AbrirConta_GravaContaEMensagemContaAbertaComTraceparent()
    {
        using var atividade = new Activity("teste-abrir-conta").Start();
        Conta conta;
        await using (var cenario = new CenarioPersistencia(banco))
        {
            conta = Conta.Abrir(await cenario.GeradorIban.GerarAsync(Ct), Titular.Criar("Ana"), Agora);
            cenario.Repositorio.Adicionar(conta);
            await cenario.UnidadeDeTrabalho.SalvarAsync(Ct);
        }

        await using var leitura = banco.CriarContexto();
        var gravada = await leitura.Contas.SingleAsync(c => c.Id == conta.Id, Ct);
        gravada.Titular.Nome.ShouldBe("Ana");
        gravada.Versao.ShouldBe(1);

        var mensagem = (await leitura.Outbox.Where(m => m.IdAgregado == conta.Id.ToString()).ToListAsync(Ct))
            .ShouldHaveSingleItem();
        mensagem.TipoAgregado.ShouldBe("contas");
        mensagem.TipoEvento.ShouldBe("ContaAberta");
        mensagem.Traceparent.ShouldBe(atividade.Id);

        using var payload = JsonDocument.Parse(mensagem.Payload);
        payload.RootElement.GetProperty("contaId").GetGuid().ShouldBe(conta.Id.Valor);
        payload.RootElement.GetProperty("iban").GetString().ShouldBe(conta.Iban.Valor);
        payload.RootElement.GetProperty("titular").GetString().ShouldBe("Ana");
        payload.RootElement.GetProperty("versao").GetInt32().ShouldBe(1);
    }

    [Fact]
    public async Task Depositar_GravaMensagemComValoresComoStringDecimalEVersao()
    {
        var conta = await CriarContaAsync("Bruno");

        await using (var cenario = new CenarioPersistencia(banco))
        {
            var carregada = await cenario.Repositorio.ObterPorIdAsync(conta.Id, Ct);
            carregada!.Depositar(Dinheiro.De(100m), Agora);
            await cenario.UnidadeDeTrabalho.SalvarAsync(Ct);
        }

        var mensagem = await UltimaMensagemAsync(conta.Id);
        mensagem.TipoEvento.ShouldBe("DepositoRealizado");
        using var payload = JsonDocument.Parse(mensagem.Payload);
        payload.RootElement.GetProperty("valor").GetString().ShouldBe("100.00");
        payload.RootElement.GetProperty("saldoApos").GetString().ShouldBe("100.00");
        payload.RootElement.GetProperty("versao").GetInt32().ShouldBe(2);
    }

    [Fact]
    public async Task Transferir_GravaAsDuasContasEAsDuasMensagensCorrelacionadas()
    {
        var origem = await CriarContaAsync("Carla", 100m);
        var destino = await CriarContaAsync("Davi");

        await using (var cenario = new CenarioPersistencia(banco))
        {
            var contaOrigem = await cenario.Repositorio.ObterPorIdAsync(origem.Id, Ct);
            var contaDestino = await cenario.Repositorio.ObterPorIbanAsync(destino.Iban, Ct);
            new ServicoTransferencia().Transferir(contaOrigem!, contaDestino!, Dinheiro.De(30m), Agora);
            await cenario.UnidadeDeTrabalho.SalvarAsync(Ct);
        }

        var enviada = await UltimaMensagemAsync(origem.Id);
        var recebida = await UltimaMensagemAsync(destino.Id);
        enviada.TipoEvento.ShouldBe("TransferenciaEnviada");
        recebida.TipoEvento.ShouldBe("TransferenciaRecebida");
        using var payloadEnviada = JsonDocument.Parse(enviada.Payload);
        using var payloadRecebida = JsonDocument.Parse(recebida.Payload);
        payloadEnviada.RootElement.GetProperty("transferenciaId").GetGuid()
            .ShouldBe(payloadRecebida.RootElement.GetProperty("transferenciaId").GetGuid());
        payloadEnviada.RootElement.GetProperty("ibanDestino").GetString().ShouldBe(destino.Iban.Valor);
        payloadRecebida.RootElement.GetProperty("ibanOrigem").GetString().ShouldBe(origem.Iban.Valor);

        await using var leitura = banco.CriarContexto();
        (await leitura.Contas.SingleAsync(c => c.Id == origem.Id, Ct)).Saldo.ShouldBe(Dinheiro.De(70m));
        (await leitura.Contas.SingleAsync(c => c.Id == destino.Id, Ct)).Saldo.ShouldBe(Dinheiro.De(30m));
    }

    [Fact]
    public async Task ComApagarAposGravar_ContaEGravadaEOutboxFicaSemALinha()
    {
        Conta conta;
        await using (var cenario = new CenarioPersistencia(banco, apagarOutboxAposGravar: true))
        {
            conta = Conta.Abrir(await cenario.GeradorIban.GerarAsync(Ct), Titular.Criar("Eva"), Agora);
            cenario.Repositorio.Adicionar(conta);
            await cenario.UnidadeDeTrabalho.SalvarAsync(Ct);
        }

        await using var leitura = banco.CriarContexto();
        (await leitura.Contas.AnyAsync(c => c.Id == conta.Id, Ct)).ShouldBeTrue();
        (await leitura.Outbox.AnyAsync(m => m.IdAgregado == conta.Id.ToString(), Ct)).ShouldBeFalse();
    }

    [Fact]
    public async Task VariosEventosNoMesmoSalvamento_SaoInseridosNaOrdemEmQueAconteceram()
    {
        // O Debezium publica na ordem do WAL; se o INSERT do depósito vier antes do ContaAberta, o Extrato
        // recebe o depósito de uma conta que ainda não conhece.
        var contas = new List<Conta>();
        await using (var cenario = new CenarioPersistencia(banco))
        {
            for (var i = 0; i < 20; i++)
            {
                var conta = Conta.Abrir(await cenario.GeradorIban.GerarAsync(Ct), Titular.Criar($"Ordem {i}"), Agora);
                conta.Depositar(Dinheiro.De(10m), Agora);
                conta.Sacar(Dinheiro.De(1m), Agora);
                cenario.Repositorio.Adicionar(conta);
                contas.Add(conta);
            }

            await cenario.UnidadeDeTrabalho.SalvarAsync(Ct);
        }

        await using var leitura = banco.CriarContexto();
        foreach (var conta in contas)
        {
            var tipos = await leitura.Outbox
                .Where(m => m.IdAgregado == conta.Id.ToString())
                .OrderBy(m => m.Sequencia)
                .Select(m => m.TipoEvento)
                .ToListAsync(Ct);

            tipos.ShouldBe(["ContaAberta", "DepositoRealizado", "SaqueRealizado"]);
        }
    }

    private async Task<Conta> CriarContaAsync(string titular, decimal saldo = 0m)
    {
        await using var cenario = new CenarioPersistencia(banco);
        var conta = Conta.Abrir(await cenario.GeradorIban.GerarAsync(Ct), Titular.Criar(titular), Agora);
        if (saldo > 0)
        {
            conta.Depositar(Dinheiro.De(saldo), Agora);
        }

        cenario.Repositorio.Adicionar(conta);
        await cenario.UnidadeDeTrabalho.SalvarAsync(Ct);
        return conta;
    }

    private async Task<Infrastructure.Outbox.MensagemOutbox> UltimaMensagemAsync(ContaId contaId)
    {
        await using var leitura = banco.CriarContexto();
        return await leitura.Outbox
            .Where(m => m.IdAgregado == contaId.ToString())
            .OrderByDescending(m => m.Id)
            .FirstAsync(Ct);
    }
}
