using System.Net.Http.Json;
using System.Text.Json;
using EuroBank.Extrato.Application.TransferObjects;
using EuroBank.Extrato.Worker.Tests.Fixtures;

namespace EuroBank.Extrato.Worker.Tests.Handlers;

/// <summary>Contas em outra moeda e os lançamentos de câmbio, do evento até a API do extrato.</summary>
public sealed class ProjecaoCambioTestes(ExtratoWorkerFixture worker)
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 9, 12, 0, 0, TimeSpan.Zero);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<JsonElement> ExtratoAsync(Guid contaId) =>
        await worker.Http.GetFromJsonAsync<JsonElement>($"/extratos/{contaId}", Ct);

    [Fact]
    public async Task ContaEmDolar_RecebeCambio_ExtratoMostraMoedaValorDeOrigemECotacao()
    {
        var contaId = Guid.CreateVersion7();
        await worker.EntregarAsync(new EventoContaAberta(contaId, "PT50EURO0000000000902", "Bruno", 1, Agora, "USD"));

        await worker.EntregarAsync(new EventoTransferenciaCambioRecebida(
            contaId, Guid.CreateVersion7(), "PT50EURO0000000000901", "Ana", null, 100m, "EUR", 93.53m, 1.0692m, 100m, 2, Agora));

        var extrato = await ExtratoAsync(contaId);
        extrato.GetProperty("moeda").GetString().ShouldBe("USD");
        extrato.GetProperty("saldoAtual").GetDecimal().ShouldBe(100m);
        var lancamento = extrato.GetProperty("lancamentos")[0];
        lancamento.GetProperty("tipo").GetString().ShouldBe("TransferenciaCambioRecebida");
        lancamento.GetProperty("moedaOrigem").GetString().ShouldBe("EUR");
        lancamento.GetProperty("valorOrigem").GetDecimal().ShouldBe(93.53m);
        lancamento.GetProperty("cotacao").GetDecimal().ShouldBe(1.0692m);
    }

    [Fact]
    public async Task CambioEnviado_ExtratoMostraOQueODestinoRecebeu()
    {
        var contaId = Guid.CreateVersion7();
        await worker.EntregarAsync(new EventoContaAberta(contaId, "PT50EURO0000000000903", "Ana", 1, Agora));
        await worker.EntregarAsync(new EventoDepositoRealizado(contaId, 1_000m, 1_000m, 2, Agora));

        await worker.EntregarAsync(new EventoTransferenciaCambioEnviada(
            contaId, Guid.CreateVersion7(), "PT50EURO0000000000904", "Bruno", null, 93.53m, 2.50m, "USD", 100m, 1.0692m, 903.97m, 3, Agora));

        var extrato = await ExtratoAsync(contaId);
        extrato.GetProperty("moeda").GetString().ShouldBe("EUR");
        var lancamento = extrato.GetProperty("lancamentos")[0];
        lancamento.GetProperty("tipo").GetString().ShouldBe("TransferenciaCambioEnviada");
        lancamento.GetProperty("tarifa").GetDecimal().ShouldBe(2.50m);
        lancamento.GetProperty("moedaDestino").GetString().ShouldBe("USD");
        lancamento.GetProperty("valorDestino").GetDecimal().ShouldBe(100m);
        lancamento.GetProperty("cotacao").GetDecimal().ShouldBe(1.0692m);
    }
}
