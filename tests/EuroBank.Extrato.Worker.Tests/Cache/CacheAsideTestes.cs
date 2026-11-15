using EuroBank.Extrato.Application.TransferObjects;
using EuroBank.Extrato.Infrastructure.Enums;
using EuroBank.Extrato.Worker.Tests.Fixtures;
using static EuroBank.Extrato.Worker.Tests.Fixtures.CenarioCache;

namespace EuroBank.Extrato.Worker.Tests.Cache;

/// <summary>Cache-aside: só a leitura enche o cache (com TTL), e cada evento apaga a entrada da conta.</summary>
[Collection(nameof(ColecaoCache))]
public sealed class CacheAsideTestes(ExtratoWorkerFixture fixture)
{
    private readonly CenarioCache _cenario = new(fixture);

    [Fact]
    public async Task Leitura_GravaComTtl_EEventoApagaAEntrada()
    {
        await using var worker = fixture.CriarWorker(EstrategiaCache.CacheAside);
        var contaId = await ContaComDepositoAsync(worker);
        (await _cenario.ResumoNoRedisAsync(contaId)).ShouldBeEmpty();

        (await ObterAsync(worker, contaId)).SaldoAtual.ShouldBe(100m);

        var validade = await _cenario.ValidadeNoRedisAsync(contaId);
        validade.ShouldNotBeNull();
        validade.Value.ShouldBeInRange(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30));

        await worker.EntregarAsync(new EventoSaqueRealizado(contaId, 30m, 70m, 3, Agora));

        (await _cenario.ResumoNoRedisAsync(contaId)).ShouldBeEmpty();
        (await ObterAsync(worker, contaId)).SaldoAtual.ShouldBe(70m);
        (await _cenario.ResumoNoRedisAsync(contaId))["versao"].ShouldBe("3");
    }

    [Fact]
    public async Task SemInvalidacao_OCacheServeODadoVelhoAteExpirar()
    {
        await using var worker = fixture.CriarWorker(EstrategiaCache.CacheAside);
        var contaId = await ContaComDepositoAsync(worker);
        await ObterAsync(worker, contaId);

        // O cache-aside confia no que tem até o TTL: um dado mexido por fora continua sendo servido.
        await _cenario.SujarCacheAsync(contaId, 1m);

        (await ObterAsync(worker, contaId)).SaldoAtual.ShouldBe(1m);
    }
}
