using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EuroBank.Contas.Api.Tests.Fixtures;
using EuroBank.Contas.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using static EuroBank.Contas.Api.Tests.Fixtures.ClienteContas;

namespace EuroBank.Contas.Api.Tests.Endpoints;

public sealed class ContasEndpointsTestes(PostgresFixture banco)
{
    private readonly ClienteContas _cliente = new(banco.Api.CreateClient());

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task AbrirConta_Retorna201ComContaAtivaLocationEOutboxContaAberta()
    {
        var resposta = await _cliente.Http.PostAsJsonAsync("/contas", new { titular = "Ana" }, Ct);

        var conta = await LerContaAsync(resposta, HttpStatusCode.Created);
        resposta.Headers.Location!.ToString().ShouldBe($"/contas/{conta.Id}");
        conta.Id.Version.ShouldBe(7);
        Iban.EhValido(conta.Iban).ShouldBeTrue();
        conta.Titular.ShouldBe("Ana");
        conta.Saldo.ShouldBe(0m);
        conta.Moeda.ShouldBe("EUR");
        conta.Status.ShouldBe("Ativa");
        conta.Versao.ShouldBe(1);

        await using var leitura = banco.CriarContexto(banco.ConnectionStringApi);
        (await leitura.Outbox.CountAsync(m => m.IdAgregado == conta.Id.ToString() && m.TipoEvento == "ContaAberta", Ct))
            .ShouldBe(1);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task AbrirConta_SemTitular_Retorna400(string? titular)
    {
        var resposta = await _cliente.Http.PostAsJsonAsync("/contas", new { titular }, Ct);

        await DeveSerProblemaAsync(resposta, HttpStatusCode.BadRequest, "titular_invalido");
    }

    [Fact]
    public async Task AbrirConta_ComTitularLongoDemais_Retorna400()
    {
        var resposta = await _cliente.Http.PostAsJsonAsync("/contas", new { titular = new string('x', 121) }, Ct);

        await DeveSerProblemaAsync(resposta, HttpStatusCode.BadRequest, "titular_invalido");
    }

    [Fact]
    public async Task Depositar_Retorna200ComSaldoEVersaoIncrementada()
    {
        var conta = await _cliente.AbrirAsync();

        var depositada = await LerContaAsync(await _cliente.DepositarAsync(conta.Id, 100m), HttpStatusCode.OK);

        depositada.Saldo.ShouldBe(100m);
        depositada.Versao.ShouldBe(2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10.555)]
    public async Task ValorInvalido_EmDepositoSaqueOuTransferencia_Retorna400(decimal valor)
    {
        var conta = await _cliente.AbrirAsync(depositoInicial: 100m);
        var destino = await _cliente.AbrirAsync();

        await DeveSerProblemaAsync(await _cliente.DepositarAsync(conta.Id, valor), HttpStatusCode.BadRequest, "valor_invalido");
        await DeveSerProblemaAsync(await _cliente.SacarAsync(conta.Id, valor), HttpStatusCode.BadRequest, "valor_invalido");
        await DeveSerProblemaAsync(
            await _cliente.TransferirAsync(conta.Id, destino.Iban, valor), HttpStatusCode.BadRequest, "valor_invalido");
        (await _cliente.ObterAsync(conta.Id)).Versao.ShouldBe(2);
    }

    [Fact]
    public async Task Sacar_ComSaldo_ReduzOSaldo()
    {
        var conta = await _cliente.AbrirAsync(depositoInicial: 100m);

        var sacada = await LerContaAsync(await _cliente.SacarAsync(conta.Id, 40m), HttpStatusCode.OK);

        sacada.Saldo.ShouldBe(60m);
    }

    [Fact]
    public async Task Sacar_SemSaldo_Retorna422ENadaMuda()
    {
        var conta = await _cliente.AbrirAsync(depositoInicial: 50m);

        await DeveSerProblemaAsync(await _cliente.SacarAsync(conta.Id, 80m), (HttpStatusCode)422, "saldo_insuficiente");

        (await _cliente.ObterAsync(conta.Id)).Saldo.ShouldBe(50m);
    }

    [Fact]
    public async Task Transferir_PeloIban_DebitaOrigemCreditaDestinoERetornaAOrigem()
    {
        var origem = await _cliente.AbrirAsync(depositoInicial: 100m);
        var destino = await _cliente.AbrirAsync();

        var resposta = await LerContaAsync(
            await _cliente.TransferirAsync(origem.Id, destino.Iban, 30m), HttpStatusCode.OK);

        resposta.Id.ShouldBe(origem.Id);
        resposta.Saldo.ShouldBe(70m);
        (await _cliente.ObterAsync(destino.Id)).Saldo.ShouldBe(30m);
    }

    [Fact]
    public async Task Transferir_ComIbanEmFormatoInvalido_Retorna400()
    {
        var origem = await _cliente.AbrirAsync(depositoInicial: 100m);

        await DeveSerProblemaAsync(
            await _cliente.TransferirAsync(origem.Id, "PT00INVALIDO", 10m), HttpStatusCode.BadRequest, "iban_invalido");
    }

    [Fact]
    public async Task Transferir_ParaIbanInexistente_Retorna404()
    {
        var origem = await _cliente.AbrirAsync(depositoInicial: 100m);
        var inexistente = Iban.Gerar(9_999_999_999_999L).Valor;

        await DeveSerProblemaAsync(
            await _cliente.TransferirAsync(origem.Id, inexistente, 10m), HttpStatusCode.NotFound, "iban_nao_encontrado");
    }

    [Fact]
    public async Task Transferir_ParaAPropriaConta_DestinoEncerradoOuSemSaldo_Retorna422ENadaMuda()
    {
        var origem = await _cliente.AbrirAsync(depositoInicial: 100m);
        var encerrada = await _cliente.AbrirAsync();
        (await _cliente.ComandoAsync(encerrada.Id, "encerramento")).StatusCode.ShouldBe(HttpStatusCode.OK);
        var destino = await _cliente.AbrirAsync();

        await DeveSerProblemaAsync(
            await _cliente.TransferirAsync(origem.Id, origem.Iban, 10m), (HttpStatusCode)422, "transferencia_para_mesma_conta");
        await DeveSerProblemaAsync(
            await _cliente.TransferirAsync(origem.Id, encerrada.Iban, 10m), (HttpStatusCode)422, "conta_encerrada");
        await DeveSerProblemaAsync(
            await _cliente.TransferirAsync(origem.Id, destino.Iban, 500m), (HttpStatusCode)422, "saldo_insuficiente");

        (await _cliente.ObterAsync(origem.Id)).Saldo.ShouldBe(100m);
        (await _cliente.ObterAsync(destino.Id)).Saldo.ShouldBe(0m);
    }

    [Fact]
    public async Task ContaBloqueada_SoRecebeCreditos()
    {
        var conta = await _cliente.AbrirAsync(depositoInicial: 100m);
        var outra = await _cliente.AbrirAsync(depositoInicial: 100m);

        var bloqueada = await LerContaAsync(await _cliente.ComandoAsync(conta.Id, "bloqueio"), HttpStatusCode.OK);

        bloqueada.Status.ShouldBe("Bloqueada");
        await DeveSerProblemaAsync(await _cliente.SacarAsync(conta.Id, 10m), (HttpStatusCode)422, "conta_bloqueada");
        await DeveSerProblemaAsync(
            await _cliente.TransferirAsync(conta.Id, outra.Iban, 10m), (HttpStatusCode)422, "conta_bloqueada");
        await DeveSerProblemaAsync(await _cliente.ComandoAsync(conta.Id, "bloqueio"), (HttpStatusCode)422, "conta_ja_bloqueada");
        (await _cliente.DepositarAsync(conta.Id, 5m)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await _cliente.TransferirAsync(outra.Id, conta.Iban, 5m)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await _cliente.ObterAsync(conta.Id)).Saldo.ShouldBe(110m);
    }

    [Fact]
    public async Task Desbloquear_ContaBloqueadaVoltaParaAtiva_E_ContaAtivaRetorna422()
    {
        var conta = await _cliente.AbrirAsync();
        await _cliente.ComandoAsync(conta.Id, "bloqueio");

        var desbloqueada = await LerContaAsync(await _cliente.ComandoAsync(conta.Id, "desbloqueio"), HttpStatusCode.OK);

        desbloqueada.Status.ShouldBe("Ativa");
        await DeveSerProblemaAsync(
            await _cliente.ComandoAsync(conta.Id, "desbloqueio"), (HttpStatusCode)422, "conta_nao_bloqueada");
    }

    [Fact]
    public async Task Encerrar_SoComSaldoZero_EContaEncerradaRejeitaOperacoes()
    {
        var comSaldo = await _cliente.AbrirAsync(depositoInicial: 1m);
        var semSaldo = await _cliente.AbrirAsync();

        await DeveSerProblemaAsync(
            await _cliente.ComandoAsync(comSaldo.Id, "encerramento"), (HttpStatusCode)422, "conta_com_saldo");
        var encerrada = await LerContaAsync(await _cliente.ComandoAsync(semSaldo.Id, "encerramento"), HttpStatusCode.OK);

        encerrada.Status.ShouldBe("Encerrada");
        await DeveSerProblemaAsync(await _cliente.DepositarAsync(semSaldo.Id, 1m), (HttpStatusCode)422, "conta_encerrada");
        await DeveSerProblemaAsync(await _cliente.ComandoAsync(semSaldo.Id, "bloqueio"), (HttpStatusCode)422, "conta_encerrada");
    }

    [Fact]
    public async Task OperacoesConcorrentesNaMesmaConta_NuncaRetornam500EOSaldoFicaConsistente()
    {
        var conta = await _cliente.AbrirAsync(depositoInicial: 100m);

        var respostas = await Task.WhenAll(
            Enumerable.Range(0, 20).Select(_ => _cliente.SacarAsync(conta.Id, 1m)));

        respostas.ShouldAllBe(r => r.StatusCode == HttpStatusCode.OK || r.StatusCode == HttpStatusCode.Conflict);
        var sucessos = respostas.Count(r => r.StatusCode == HttpStatusCode.OK);
        foreach (var conflito in respostas.Where(r => r.StatusCode == HttpStatusCode.Conflict))
        {
            await DeveSerProblemaAsync(conflito, HttpStatusCode.Conflict, "conflito_de_concorrencia");
        }

        (await _cliente.ObterAsync(conta.Id)).Saldo.ShouldBe(100m - sucessos);
    }

    [Theory]
    [InlineData("GET", "")]
    [InlineData("POST", "/depositos")]
    [InlineData("POST", "/saques")]
    [InlineData("POST", "/transferencias")]
    [InlineData("POST", "/bloqueio")]
    [InlineData("POST", "/desbloqueio")]
    [InlineData("POST", "/encerramento")]
    public async Task ContaInexistente_Retorna404(string verbo, string sufixo)
    {
        var destino = await _cliente.AbrirAsync();
        var requisicao = new HttpRequestMessage(new HttpMethod(verbo), $"/contas/{Guid.CreateVersion7()}{sufixo}");
        if (verbo == "POST")
        {
            requisicao.Content = JsonContent.Create(new { valor = 10m, ibanDestino = destino.Iban });
        }

        var resposta = await _cliente.Http.SendAsync(requisicao, Ct);

        await DeveSerProblemaAsync(resposta, HttpStatusCode.NotFound, "conta_nao_encontrada");
    }

    [Fact]
    public async Task ListarContas_RetornaPaginaComTotal()
    {
        await _cliente.AbrirAsync();
        await _cliente.AbrirAsync();

        using var pagina = JsonDocument.Parse(await _cliente.Http.GetStringAsync("/contas?pagina=1&tamanho=2", Ct));

        pagina.RootElement.GetProperty("pagina").GetInt32().ShouldBe(1);
        pagina.RootElement.GetProperty("tamanho").GetInt32().ShouldBe(2);
        pagina.RootElement.GetProperty("itens").GetArrayLength().ShouldBe(2);
        pagina.RootElement.GetProperty("total").GetInt64().ShouldBeGreaterThanOrEqualTo(2);
    }

    [Theory]
    [InlineData("pagina=0")]
    [InlineData("tamanho=0")]
    [InlineData("tamanho=201")]
    public async Task ListarContas_ComPaginacaoInvalida_Retorna400(string consulta)
    {
        var resposta = await _cliente.Http.GetAsync($"/contas?{consulta}", Ct);

        await DeveSerProblemaAsync(resposta, HttpStatusCode.BadRequest, "paginacao_invalida");
    }
}
