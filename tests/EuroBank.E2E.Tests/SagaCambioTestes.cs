using System.Net;
using System.Net.Http.Json;
using EuroBank.E2E.Tests.Fixtures;
using static EuroBank.E2E.Tests.Fixtures.CenarioSaga;

namespace EuroBank.E2E.Tests;

/// <summary>
/// Sagas com câmbio, com Debezium de verdade: cotação no câmbio simulado, débito na moeda da conta, liquidação ou
/// crédito na moeda de destino, e o extrato dos dois lados com a cotação usada.
/// </summary>
public sealed class SagaCambioTestes(LaboratorioFixture lab)
{
    private static readonly TimeSpan Limite = TimeSpan.FromSeconds(60);

    private readonly CenarioSaga _cenario = new(lab);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ExternaEmDolarDeContaEmEuro_CotaDebitaOEquivalenteComSpread_EOExtratoMostraOCambio()
    {
        var conta = await _cenario.AbrirContaAsync(1_000m);

        var pedido = await _cenario.SolicitarComSucessoAsync(conta, "00", 100m, moedaDestino: "USD", moedaDoValor: "USD");
        var concluida = await _cenario.EsperarEstadoAsync(pedido.Id, "Concluida", Limite);

        // 100 USD a 1,08 com 1% de spread: 1 EUR = 1,0692 USD, então saem 93,53 EUR mais 2,50 de tarifa.
        concluida.Cotacao.ShouldNotBeNull().Taxa.ShouldBe(1.0692m);
        concluida.ValorDebitado.ShouldBe(93.53m);
        concluida.ValorDestino.ShouldBe(100m);
        (await _cenario.SaldoAsync(conta)).ShouldBe(903.97m);

        var lancamento = (await _cenario.EsperarLancamentosAsync(conta, 2, Limite))[^1];
        lancamento.GetProperty("tipo").GetString().ShouldBe("TransferenciaExternaEnviada");
        lancamento.GetProperty("valor").GetDecimal().ShouldBe(93.53m);
        lancamento.GetProperty("moedaDestino").GetString().ShouldBe("USD");
        lancamento.GetProperty("valorDestino").GetDecimal().ShouldBe(100m);
        lancamento.GetProperty("cotacao").GetDecimal().ShouldBe(1.0692m);
    }

    [Fact]
    public async Task ParBrlGbp_TerminaRecusadaPorCambioIndisponivel_SemDebito()
    {
        var (conta, _) = await _cenario.AbrirContaEmAsync("BRL", 1_000m);

        var pedido = await _cenario.SolicitarComSucessoAsync(conta, "00", 100m, moedaDestino: "GBP");
        var recusada = await _cenario.EsperarEstadoAsync(pedido.Id, "Recusada", Limite);

        recusada.Motivo.ShouldBe("CambioIndisponivel");
        (await _cenario.SaldoAsync(conta)).ShouldBe(1_000m);
    }

    [Fact]
    public async Task CambioInterno_DebitaAOrigemEmEuro_CreditaODestinoEmDolar_EConclui()
    {
        var origem = await _cenario.AbrirContaAsync(1_000m);
        var (destino, ibanDestino) = await _cenario.AbrirContaEmAsync("USD");

        var pedido = await _cenario.SolicitarCambioComSucessoAsync(origem, ibanDestino, 100m, "USD");
        await _cenario.EsperarEstadoAsync(pedido.Id, "Concluida", Limite);

        (await _cenario.SaldoAsync(origem)).ShouldBe(903.97m);
        (await _cenario.SaldoAsync(destino)).ShouldBe(100m);

        var enviado = (await _cenario.EsperarLancamentosAsync(origem, 2, Limite))[^1];
        enviado.GetProperty("tipo").GetString().ShouldBe("TransferenciaCambioEnviada");
        enviado.GetProperty("valorDestino").GetDecimal().ShouldBe(100m);
        var recebido = (await _cenario.EsperarLancamentosAsync(destino, 1, Limite))[^1];
        recebido.GetProperty("tipo").GetString().ShouldBe("TransferenciaCambioRecebida");
        recebido.GetProperty("moedaOrigem").GetString().ShouldBe("EUR");
        recebido.GetProperty("valorOrigem").GetDecimal().ShouldBe(93.53m);
    }

    [Fact]
    public async Task CambioInterno_ComODestinoEncerrado_EstornaAOrigem()
    {
        var origem = await _cenario.AbrirContaAsync(1_000m);
        var (destino, ibanDestino) = await _cenario.AbrirContaEmAsync("USD");
        (await _cenario.ComandoContaAsync(destino, "encerramento")).EnsureSuccessStatusCode();

        var pedido = await _cenario.SolicitarCambioComSucessoAsync(origem, ibanDestino, 100m, "USD");
        var cancelada = await _cenario.EsperarEstadoAsync(pedido.Id, "Cancelada", Limite);

        cancelada.Motivo.ShouldBe("DestinoEncerrado");
        (await _cenario.SaldoAsync(origem)).ShouldBe(1_000m);
    }

    [Fact]
    public async Task TransferenciaSincronaEntreMoedas_Responde422IndicandoOCambio()
    {
        var origem = await _cenario.AbrirContaAsync(100m);
        var (_, ibanDestino) = await _cenario.AbrirContaEmAsync("USD");

        var resposta = await lab.Contas.PostAsJsonAsync($"/contas/{origem}/transferencias", new { ibanDestino, valor = 10m }, Ct);

        await DeveSerProblemaAsync(resposta, HttpStatusCode.UnprocessableEntity, "transferencia_entre_moedas");
        (await resposta.Content.ReadAsStringAsync(Ct)).ShouldContain("/transferencias-cambio");
    }
}
