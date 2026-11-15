using EuroBank.Extrato.Application.TransferObjects;
using EuroBank.Extrato.Infrastructure.Cache.WriteBehind;
using EuroBank.Extrato.Infrastructure.Enums;
using EuroBank.Extrato.Worker.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using static EuroBank.Extrato.Worker.Tests.Fixtures.CenarioCache;

namespace EuroBank.Extrato.Worker.Tests.Cache;

/// <summary>Write-behind: o evento vai só para o Redis; o Postgres recebe depois, na descarga das pendências.</summary>
[Collection(nameof(ColecaoCache))]
public sealed class CacheWriteBehindTestes(ExtratoWorkerFixture fixture)
{
    private readonly CenarioCache _cenario = new(fixture);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static Task<int> DescarregarAsync(WorkerExtrato worker) =>
        worker.Servicos.GetRequiredService<DescargaPendencias>().DescarregarAsync(Ct);

    [Fact]
    public async Task Evento_VaiParaORedis_EOPostgresSoRecebeNaDescarga()
    {
        await using var worker = fixture.CriarWorker(EstrategiaCache.WriteBehind);
        await DescarregarAsync(worker);

        var contaId = await ContaComDepositoAsync(worker);

        (await _cenario.NoPostgresAsync(contaId)).ShouldBeNull();
        var extrato = await ObterAsync(worker, contaId);
        extrato.SaldoAtual.ShouldBe(100m);
        extrato.Lancamentos.ShouldHaveSingleItem().Tipo.ShouldBe("Deposito");

        (await DescarregarAsync(worker)).ShouldBe(2);

        (await _cenario.NoPostgresAsync(contaId)).ShouldBe((2, 100m, 1));
        (await _cenario.PendenciasNoRedisAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task EventoRepetidoEDescargaRepetida_NaoDuplicamNada()
    {
        await using var worker = fixture.CriarWorker(EstrategiaCache.WriteBehind);
        var contaId = await ContaComDepositoAsync(worker);
        await DescarregarAsync(worker);

        await worker.EntregarAsync(new EventoDepositoRealizado(contaId, 100m, 100m, 2, Agora));
        await worker.EntregarAsync(new EventoSaqueRealizado(contaId, 40m, 60m, 3, Agora));
        await DescarregarAsync(worker);
        await DescarregarAsync(worker);

        (await _cenario.NoPostgresAsync(contaId)).ShouldBe((3, 60m, 2));
        (await _cenario.ResumoNoRedisAsync(contaId))["total"].ShouldBe("2");
    }

    [Fact]
    public async Task ContaQueSoEstavaNoPostgres_EntraNoRedisComOHistorico()
    {
        Guid contaId;
        await using (var semCache = fixture.CriarWorker(EstrategiaCache.Nenhuma))
        {
            contaId = await ContaComDepositoAsync(semCache);
        }

        await using var worker = fixture.CriarWorker(EstrategiaCache.WriteBehind);
        await worker.EntregarAsync(new EventoSaqueRealizado(contaId, 40m, 60m, 3, Agora));

        var extrato = await ObterAsync(worker, contaId);
        extrato.SaldoAtual.ShouldBe(60m);
        extrato.Total.ShouldBe(2);
        extrato.Lancamentos.Select(l => l.Tipo).ShouldBe(["Saque", "Deposito"]);

        await DescarregarAsync(worker);
        (await _cenario.NoPostgresAsync(contaId)).ShouldBe((3, 60m, 2));
    }
}
