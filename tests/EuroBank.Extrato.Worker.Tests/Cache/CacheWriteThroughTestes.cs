using System.Diagnostics.Metrics;
using EuroBank.Extrato.Application.TransferObjects;
using EuroBank.Extrato.Infrastructure.Cache;
using EuroBank.Extrato.Infrastructure.Enums;
using EuroBank.Extrato.Worker.Tests.Fixtures;
using static EuroBank.Extrato.Worker.Tests.Fixtures.CenarioCache;

namespace EuroBank.Extrato.Worker.Tests.Cache;

/// <summary>Write-through: o evento grava no Postgres e no Redis; a leitura logo depois já vem do cache, atualizada.</summary>
[Collection(nameof(ColecaoCache))]
public sealed class CacheWriteThroughTestes(ExtratoWorkerFixture fixture)
{
    private readonly CenarioCache _cenario = new(fixture);

    [Fact]
    public async Task ContaNova_EntraNoCacheSemEsperarUmaLeitura()
    {
        await using var worker = fixture.CriarWorker(EstrategiaCache.WriteThrough);

        var contaId = await ContaComDepositoAsync(worker);

        var resumo = await _cenario.ResumoNoRedisAsync(contaId);
        resumo["versao"].ShouldBe("2");
        resumo["saldo"].ShouldBe("100.00");
        resumo["total"].ShouldBe("1");
        (await _cenario.ValidadeNoRedisAsync(contaId)).ShouldBeNull();
    }

    [Fact]
    public async Task LeituraLogoDepoisDoEvento_VemDoRedisComOSaldoNovo()
    {
        await using var worker = fixture.CriarWorker(EstrategiaCache.WriteThrough);
        var contaId = await ContaComDepositoAsync(worker);
        using var medicoes = new Medicoes();

        await worker.EntregarAsync(new EventoSaqueRealizado(contaId, 30m, 70m, 3, Agora));
        var extrato = await ObterAsync(worker, contaId);

        extrato.SaldoAtual.ShouldBe(70m);
        extrato.UltimaVersao.ShouldBe(3);
        extrato.Total.ShouldBe(2);
        extrato.Lancamentos.Select(l => l.Tipo).ShouldBe(["Saque", "Deposito"]);
        medicoes.Acertos.ShouldBe(1);
        medicoes.Faltas.ShouldBe(0);
    }

    [Fact]
    public async Task EventoRepetido_NaoMudaOCache()
    {
        await using var worker = fixture.CriarWorker(EstrategiaCache.WriteThrough);
        var contaId = await ContaComDepositoAsync(worker);

        await worker.EntregarAsync(new EventoDepositoRealizado(contaId, 100m, 100m, 2, Agora));

        (await _cenario.ResumoNoRedisAsync(contaId))["total"].ShouldBe("1");
        (await ObterAsync(worker, contaId)).Lancamentos.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task PaginaAlemDoCache_VemDoPostgres()
    {
        await using var worker = fixture.CriarWorker(EstrategiaCache.WriteThrough);
        var contaId = await ContaComDepositoAsync(worker);
        await _cenario.SujarCacheAsync(contaId, 999m);

        // 100 lançamentos por página não cabem nos 50 guardados: a leitura vai ao Postgres, que tem o saldo certo.
        var extrato = await ObterAsync(worker, contaId, "?tamanho=100");

        extrato.SaldoAtual.ShouldBe(100m);
        (await ObterAsync(worker, contaId)).SaldoAtual.ShouldBe(999m);
    }

    /// <summary>Conta os acertos e as faltas do cache enquanto existe.</summary>
    private sealed class Medicoes : IDisposable
    {
        private readonly MeterListener _ouvinte = new();

        public Medicoes()
        {
            _ouvinte.InstrumentPublished = (instrumento, ouvinte) =>
            {
                if (instrumento.Meter.Name == MetricasCache.NomeMedidor)
                {
                    ouvinte.EnableMeasurementEvents(instrumento);
                }
            };
            _ouvinte.SetMeasurementEventCallback<long>((instrumento, valor, _, _) =>
            {
                if (instrumento.Name == "extrato.cache.hits")
                {
                    Interlocked.Add(ref _acertos, valor);
                }
                else if (instrumento.Name == "extrato.cache.misses")
                {
                    Interlocked.Add(ref _faltas, valor);
                }
            });
            _ouvinte.Start();
        }

        private long _acertos;
        private long _faltas;

        public long Acertos => Interlocked.Read(ref _acertos);

        public long Faltas => Interlocked.Read(ref _faltas);

        public void Dispose() => _ouvinte.Dispose();
    }
}
