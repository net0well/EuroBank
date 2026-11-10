using System.Net;
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

/// <summary>Contas em várias moedas e os comandos da saga de câmbio, contra o Postgres real.</summary>
public sealed class ComandosCambioTestes(PostgresFixture banco)
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 9, 12, 0, 0, TimeSpan.Zero);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<Conta> CriarContaAsync(Moeda moeda, decimal saldo = 0m, bool encerrar = false)
    {
        await using var cenario = new CenarioPersistencia(banco);
        var conta = Conta.Abrir(await cenario.GeradorIban.GerarAsync(Ct), Titular.Criar("Câmbio"), Agora, moeda);
        if (saldo > 0)
        {
            conta.Depositar(Dinheiro.De(saldo, moeda), Agora);
        }

        if (encerrar)
        {
            conta.Encerrar(Agora);
        }

        cenario.Repositorio.Adicionar(conta);
        await cenario.UnidadeDeTrabalho.SalvarAsync(Ct);
        return conta;
    }

    private static PassoDaSaga Passo(CenarioPersistencia cenario) =>
        new(cenario.Repositorio, cenario.UnidadeDeTrabalho, NullLogger<PassoDaSaga>.Instance);

    private async Task DebitarCambioAsync(Guid transferenciaId, Conta conta, string moeda = "EUR")
    {
        await using var cenario = new CenarioPersistencia(banco);
        await new DebitarTransferenciaCambioManipulador(Passo(cenario), new RespostasSagaOutbox(cenario.Fila, TimeProvider.System), TimeProvider.System)
            .Handle(new DebitarTransferenciaCambio(
                transferenciaId, conta.Id.Valor, 93.53m, 2.50m, moeda, "PT50EURO0000000000002", "Bruno", null, "USD", 100m, 1.0692m), Ct);
    }

    private async Task CreditarCambioAsync(Guid transferenciaId, Conta conta)
    {
        await using var cenario = new CenarioPersistencia(banco);
        await new CreditarTransferenciaCambioManipulador(Passo(cenario), new RespostasSagaOutbox(cenario.Fila, TimeProvider.System), TimeProvider.System)
            .Handle(new CreditarTransferenciaCambio(
                transferenciaId, conta.Id.Valor, 100m, "USD", "PT50EURO0000000000001", "Ana", null, "EUR", 93.53m, 1.0692m), Ct);
    }

    private async Task<List<MensagemOutbox>> MensagensAsync(params string[] idsAgregado)
    {
        await using var leitura = banco.CriarContexto();
        return await leitura.Outbox.Where(m => idsAgregado.Contains(m.IdAgregado)).OrderBy(m => m.Sequencia).ToListAsync(Ct);
    }

    [Fact]
    public async Task ContaEmDolar_PersisteAMoedaESaldoNaMoeda_EOEventoDeAberturaLevaAMoeda()
    {
        var conta = await CriarContaAsync(Moeda.USD, 50m);

        await using var leitura = banco.CriarContexto();
        var gravada = await leitura.Contas.SingleAsync(c => c.Id == conta.Id, Ct);
        gravada.Moeda.ShouldBe(Moeda.USD);
        gravada.Saldo.ShouldBe(Dinheiro.De(50m, Moeda.USD));
        var abertura = (await MensagensAsync(conta.Id.ToString())).First(m => m.TipoEvento == "ContaAberta");
        using var payload = JsonDocument.Parse(abertura.Payload);
        payload.RootElement.GetProperty("moeda").GetString().ShouldBe("USD");
    }

    [Fact]
    public async Task DebitoECreditoDeCambio_MovemAsDuasMoedasERespondemASaga()
    {
        var origem = await CriarContaAsync(Moeda.EUR, 1_000m);
        var destino = await CriarContaAsync(Moeda.USD);
        var transferenciaId = Guid.CreateVersion7();

        await DebitarCambioAsync(transferenciaId, origem);
        await CreditarCambioAsync(transferenciaId, destino);
        await CreditarCambioAsync(transferenciaId, destino);

        await using var leitura = banco.CriarContexto();
        (await leitura.Contas.SingleAsync(c => c.Id == origem.Id, Ct)).Saldo.ShouldBe(Dinheiro.De(903.97m));
        (await leitura.Contas.SingleAsync(c => c.Id == destino.Id, Ct)).Saldo.ShouldBe(Dinheiro.De(100m, Moeda.USD));

        var mensagens = await MensagensAsync(origem.Id.ToString(), destino.Id.ToString(), transferenciaId.ToString());
        var enviada = mensagens.Single(m => m.TipoEvento == "TransferenciaCambioEnviada");
        using (var payload = JsonDocument.Parse(enviada.Payload))
        {
            payload.RootElement.GetProperty("moedaDestino").GetString().ShouldBe("USD");
            payload.RootElement.GetProperty("valorDestino").GetString().ShouldBe("100.00");
            payload.RootElement.GetProperty("cotacao").GetString().ShouldBe("1.0692");
        }

        mensagens.Count(m => m.TipoEvento == "TransferenciaCambioRecebida").ShouldBe(1);
        mensagens.Where(m => m.TipoAgregado == "contas.respostas").Select(m => m.TipoEvento)
            .ShouldBe(["DebitoRealizado", "CreditoRealizado", "CreditoRealizado"]);
    }

    [Fact]
    public async Task CreditoEmContaEncerrada_RespondeCreditoRecusado()
    {
        var destino = await CriarContaAsync(Moeda.USD, encerrar: true);
        var transferenciaId = Guid.CreateVersion7();

        await CreditarCambioAsync(transferenciaId, destino);

        var resposta = (await MensagensAsync(transferenciaId.ToString())).ShouldHaveSingleItem();
        resposta.TipoEvento.ShouldBe("CreditoRecusado");
        using var payload = JsonDocument.Parse(resposta.Payload);
        payload.RootElement.GetProperty("motivo").GetString().ShouldBe("ContaEncerrada");
    }

    [Fact]
    public async Task DebitoComMoedaDiferenteDaConta_RespondeDebitoRecusado()
    {
        var origem = await CriarContaAsync(Moeda.GBP, 1_000m);
        var transferenciaId = Guid.CreateVersion7();

        await DebitarCambioAsync(transferenciaId, origem, moeda: "EUR");

        using var payload = JsonDocument.Parse((await MensagensAsync(transferenciaId.ToString())).ShouldHaveSingleItem().Payload);
        payload.RootElement.GetProperty("motivo").GetString().ShouldBe("MoedaDiferente");
    }

    [Fact]
    public async Task AbrirPelaApi_EmDolar_EComMoedaInvalida()
    {
        using var http = banco.Api.CriarClienteAutenticado();
        var cliente = new ClienteContas(http);

        (await cliente.AbrirAsync(moeda: "usd")).Moeda.ShouldBe("USD");
        (await cliente.AbrirAsync()).Moeda.ShouldBe("EUR");
        await ClienteContas.DeveSerProblemaAsync(
            await http.PostAsync("/contas", System.Net.Http.Json.JsonContent.Create(new { titular = "X", moeda = "JPY" }), Ct),
            HttpStatusCode.BadRequest,
            "moeda_invalida");
    }

    [Fact]
    public async Task TransferenciaSincronaEntreMoedas_Responde422IndicandoOCambio()
    {
        using var http = banco.Api.CriarClienteAutenticado();
        var cliente = new ClienteContas(http);
        var euros = await cliente.AbrirAsync(depositoInicial: 100m);
        var dolares = await cliente.AbrirAsync(moeda: "USD");

        var resposta = await cliente.TransferirAsync(euros.Id, dolares.Iban, 10m);

        await ClienteContas.DeveSerProblemaAsync(resposta, (HttpStatusCode)422, "transferencia_entre_moedas");
        (await cliente.ObterAsync(euros.Id)).Saldo.ShouldBe(100m);
    }
}
