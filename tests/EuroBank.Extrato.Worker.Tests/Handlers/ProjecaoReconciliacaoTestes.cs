using System.Net.Http.Json;
using System.Text.Json;
using EuroBank.Extrato.Application.TransferObjects;
using EuroBank.Extrato.Worker.Tests.Fixtures;

namespace EuroBank.Extrato.Worker.Tests.Handlers;

/// <summary>Devolução e reconciliação no extrato, inclusive com o saldo ficando negativo.</summary>
public sealed class ProjecaoReconciliacaoTestes(ExtratoWorkerFixture worker)
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 10, 12, 0, 0, TimeSpan.Zero);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ReconciliacaoComSaldoNegativo_EDevolucao_AparecemNoExtrato()
    {
        var contaId = Guid.CreateVersion7();
        var transferenciaId = Guid.CreateVersion7();
        await worker.EntregarAsync(new EventoContaAberta(contaId, "PT50EURO0000000000905", "Ana", 1, Agora));
        await worker.EntregarAsync(new EventoDepositoRealizado(contaId, 50m, 50m, 2, Agora));

        await worker.EntregarAsync(new EventoDebitoReconciliacao(contaId, transferenciaId, "Bruno", 102.50m, -52.50m, 3, Agora));
        await worker.EntregarAsync(new EventoDevolucaoTransferenciaExterna(contaId, Guid.CreateVersion7(), "Carla", 100m, 47.50m, 4, Agora));

        var extrato = await worker.Http.GetFromJsonAsync<JsonElement>($"/extratos/{contaId}", Ct);
        extrato.GetProperty("saldoAtual").GetDecimal().ShouldBe(47.50m);
        var lancamentos = extrato.GetProperty("lancamentos");
        lancamentos[0].GetProperty("tipo").GetString().ShouldBe("DevolucaoTransferenciaExterna");
        lancamentos[1].GetProperty("tipo").GetString().ShouldBe("DebitoReconciliacao");
        lancamentos[1].GetProperty("saldoApos").GetDecimal().ShouldBe(-52.50m);
    }
}
