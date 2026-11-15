using EuroBank.Extrato.Application.TransferObjects;
using EuroBank.Extrato.Infrastructure.Cache;
using EuroBank.Extrato.Infrastructure.Enums;
using EuroBank.Extrato.Worker.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using static EuroBank.Extrato.Worker.Tests.Fixtures.CenarioCache;

namespace EuroBank.Extrato.Worker.Tests.Cache;

/// <summary>
/// Redis fora do ar: o extrato segue pelo Postgres e o cache fica marcado como suspeito; com o Redis de volta, as
/// chaves são apagadas e a marca sai.
/// </summary>
[Collection(nameof(ColecaoCache))]
public sealed class CacheSuspeitoTestes(ExtratoWorkerFixture fixture) : IAsyncLifetime
{
    private readonly CenarioCache _cenario = new(fixture);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public ValueTask InitializeAsync() => new(_cenario.LimparMarcaAsync());

    public ValueTask DisposeAsync() => new(_cenario.LimparMarcaAsync());

    [Theory]
    [InlineData(EstrategiaCache.WriteThrough)]
    [InlineData(EstrategiaCache.CacheAside)]
    [InlineData(EstrategiaCache.WriteBehind)]
    public async Task RedisFora_OEventoChegaAoPostgres_EOCacheFicaSuspeito(EstrategiaCache estrategia)
    {
        await using var worker = fixture.CriarWorker(estrategia, ExtratoWorkerFixture.RedisForaDoAr);

        var contaId = await ContaComDepositoAsync(worker);

        (await _cenario.NoPostgresAsync(contaId)).ShouldBe((2, 100m, 1));
        (await _cenario.SuspeitoDesdeAsync()).ShouldNotBeNull();
        (await ObterAsync(worker, contaId)).SaldoAtual.ShouldBe(100m);
    }

    [Fact]
    public async Task Suspeito_LeituraIgnoraORedis_EReconstrucaoApagaAsChavesETiraAMarca()
    {
        await using var worker = fixture.CriarWorker(EstrategiaCache.WriteThrough);
        var contaId = await ContaComDepositoAsync(worker);
        await _cenario.SujarCacheAsync(contaId, 999m);
        (await ObterAsync(worker, contaId)).SaldoAtual.ShouldBe(999m);

        await using (var semRedis = fixture.CriarWorker(EstrategiaCache.WriteThrough, ExtratoWorkerFixture.RedisForaDoAr))
        {
            await semRedis.EntregarAsync(new EventoSaqueRealizado(contaId, 30m, 70m, 3, Agora));
        }

        // O Redis ficou com o saldo de antes; com a marca, a leitura vai ao Postgres.
        (await _cenario.ResumoNoRedisAsync(contaId))["saldo"].ShouldBe("999");
        (await ObterAsync(worker, contaId)).SaldoAtual.ShouldBe(70m);

        (await worker.Servicos.GetRequiredService<ReconstrucaoCache>().TentarAsync(Ct)).ShouldBeTrue();

        (await _cenario.SuspeitoDesdeAsync()).ShouldBeNull();
        (await _cenario.ResumoNoRedisAsync(contaId)).ShouldBeEmpty();
        (await ObterAsync(worker, contaId)).SaldoAtual.ShouldBe(70m);
        (await _cenario.ResumoNoRedisAsync(contaId))["saldo"].ShouldBe("70.00");
    }

    [Fact]
    public async Task SemMarca_ReconstrucaoNaoFazNada()
    {
        await using var worker = fixture.CriarWorker(EstrategiaCache.WriteThrough);
        var contaId = await ContaComDepositoAsync(worker);

        (await worker.Servicos.GetRequiredService<ReconstrucaoCache>().TentarAsync(Ct)).ShouldBeFalse();

        (await _cenario.ResumoNoRedisAsync(contaId)).ShouldNotBeEmpty();
    }

    [Fact]
    public async Task SemCache_NadaVaiParaORedis()
    {
        await using var worker = fixture.CriarWorker(EstrategiaCache.Nenhuma);
        var contaId = await ContaComDepositoAsync(worker);

        (await ObterAsync(worker, contaId)).SaldoAtual.ShouldBe(100m);

        (await _cenario.ResumoNoRedisAsync(contaId)).ShouldBeEmpty();
    }
}
