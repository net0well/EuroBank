using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EuroBank.E2E.Tests.Fixtures;
using EuroBank.Extrato.Infrastructure.Cache;

namespace EuroBank.E2E.Tests;

/// <summary>
/// O Redis no restante do laboratório, com Debezium e Kafka de verdade: o extrato em write-through servido do cache
/// logo depois do evento, e a Idempotency-Key reservada no Redis com pedidos em paralelo.
/// </summary>
public sealed class RedisLaboratorioTestes(LaboratorioFixture lab)
{
    private static readonly TimeSpan Limite = TimeSpan.FromSeconds(60);

    private readonly CenarioSaga _cenario = new(lab);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<Dictionary<string, string>> ResumoNoRedisAsync(Guid contaId) =>
        (await lab.Redis.GetDatabase().HashGetAllAsync(ChavesExtrato.Resumo(contaId)))
        .ToDictionary(campo => campo.Name.ToString(), campo => campo.Value.ToString(), StringComparer.Ordinal);

    [Fact]
    public async Task WriteThrough_OEventoAtualizaORedis_EALeituraLogoDepoisJaTrazOSaldoNovo()
    {
        var conta = await _cenario.AbrirContaAsync(100m);
        await _cenario.EsperarLancamentosAsync(conta, 1, Limite);

        (await lab.Contas.PostAsJsonAsync($"/contas/{conta}/depositos", new { valor = 50m }, Ct)).EnsureSuccessStatusCode();

        // O manipulador do Extrato grava no Postgres e no Redis; quando o Redis tem a versão nova, a leitura sai de lá.
        await Esperar.AteAsync(
            async () => (await ResumoNoRedisAsync(conta)).GetValueOrDefault("saldo") == "150.00",
            Limite,
            $"o Redis não recebeu o saldo novo da conta {conta}");
        var resumo = await ResumoNoRedisAsync(conta);
        var extrato = await lab.Extrato.GetFromJsonAsync<JsonElement>($"/extratos/{conta}", Ct);

        extrato.GetProperty("saldoAtual").GetDecimal().ShouldBe(150m);
        extrato.GetProperty("ultimaVersao").GetInt32().ToString(System.Globalization.CultureInfo.InvariantCulture).ShouldBe(resumo["versao"]);
        extrato.GetProperty("lancamentos").GetArrayLength().ShouldBe(2);
        resumo["total"].ShouldBe("2");
    }

    [Fact]
    public async Task MesmaIdempotencyKeyEmParalelo_CriaUmaTransferenciaSo()
    {
        var conta = await _cenario.AbrirContaAsync(1_000m);
        var chave = Guid.CreateVersion7().ToString();

        // O primeiro pedido espera a conta chegar à projeção de Transferências; os outros vão juntos depois.
        var primeira = await CenarioSaga.LerAsync(await _cenario.SolicitarAsync(conta, "00", 100m, idempotencyKey: chave), HttpStatusCode.Accepted);
        var respostas = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => _cenario.SolicitarAsync(conta, "00", 100m, idempotencyKey: chave)));

        respostas.ShouldAllBe(resposta => resposta.StatusCode == HttpStatusCode.Accepted);
        foreach (var resposta in respostas)
        {
            (await CenarioSaga.LerAsync(resposta, HttpStatusCode.Accepted)).Id.ShouldBe(primeira.Id);
        }

        (await lab.Redis.GetDatabase().HashGetAsync($"idempotencia:transferencias:{chave}", "estado")).ToString().ShouldBe("concluida");
        await _cenario.EsperarEstadoAsync(primeira.Id, "Concluida", Limite);
        (await _cenario.SaldoAsync(conta)).ShouldBe(1_000m - 102.50m);
    }
}
