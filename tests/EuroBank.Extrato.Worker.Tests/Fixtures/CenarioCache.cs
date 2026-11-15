using System.Net.Http.Json;
using EuroBank.Extrato.Application.TransferObjects;
using EuroBank.Extrato.Infrastructure.Cache;
using EuroBank.Extrato.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace EuroBank.Extrato.Worker.Tests.Fixtures;

/// <summary>Atalhos dos testes do cache: eventos de uma conta nova, a leitura pela API e o que ficou no Redis e no Postgres.</summary>
public sealed class CenarioCache(ExtratoWorkerFixture fixture)
{
    public static readonly DateTimeOffset Agora = new(2026, 11, 14, 12, 0, 0, TimeSpan.Zero);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Abre uma conta e deposita 100 (versões 1 e 2).</summary>
    public static async Task<Guid> ContaComDepositoAsync(WorkerExtrato worker)
    {
        ArgumentNullException.ThrowIfNull(worker);
        var contaId = Guid.CreateVersion7();
        await worker.EntregarAsync(new EventoContaAberta(contaId, "PT53EURO0000000000001", "Ana", 1, Agora));
        await worker.EntregarAsync(new EventoDepositoRealizado(contaId, 100m, 100m, 2, Agora));
        return contaId;
    }

    public static async Task<ExtratoDto> ObterAsync(WorkerExtrato worker, Guid contaId, string consulta = "")
    {
        ArgumentNullException.ThrowIfNull(worker);
        return (await worker.Http.GetFromJsonAsync<ExtratoDto>($"/extratos/{contaId}{consulta}", Ct))!;
    }

    /// <summary>O resumo guardado no Redis (vazio se a conta não está lá).</summary>
    public async Task<Dictionary<string, string>> ResumoNoRedisAsync(Guid contaId) =>
        (await fixture.Redis.GetDatabase().HashGetAllAsync(ChavesExtrato.Resumo(contaId)))
        .ToDictionary(campo => campo.Name.ToString(), campo => campo.Value.ToString(), StringComparer.Ordinal);

    public Task<TimeSpan?> ValidadeNoRedisAsync(Guid contaId) => fixture.Redis.GetDatabase().KeyTimeToLiveAsync(ChavesExtrato.Resumo(contaId));

    /// <summary>Grava direto no Redis um saldo que o Postgres não tem, como um cache que ficou para trás.</summary>
    public Task SujarCacheAsync(Guid contaId, decimal saldo) =>
        fixture.Redis.GetDatabase().HashSetAsync(ChavesExtrato.Resumo(contaId), [new HashEntry("saldo", saldo.ToString(System.Globalization.CultureInfo.InvariantCulture))]);

    public async Task<(int Versao, decimal Saldo, int Lancamentos)?> NoPostgresAsync(Guid contaId)
    {
        await using var contexto = fixture.CriarContexto();
        var extrato = await contexto.Extratos.AsNoTracking().SingleOrDefaultAsync(e => e.ContaId == contaId, Ct);
        if (extrato is null)
        {
            return null;
        }

        var lancamentos = await contexto.Lancamentos.CountAsync(l => EF.Property<Guid>(l, "ContaId") == contaId, Ct);
        return (extrato.UltimaVersao, extrato.SaldoAtual.Valor, lancamentos);
    }

    public async Task<DateTimeOffset?> SuspeitoDesdeAsync()
    {
        await using var contexto = fixture.CriarContexto();
        return await contexto.MarcasCache.Where(m => m.Id == MarcaCache.IdUnico).Select(m => m.SuspeitoDesde).SingleAsync(Ct);
    }

    /// <summary>Tira a marca no Postgres (os workers voltam a ler o Redis em até um segundo).</summary>
    public async Task LimparMarcaAsync()
    {
        await using var contexto = fixture.CriarContexto();
        await contexto.MarcasCache.ExecuteUpdateAsync(m => m.SetProperty(x => x.SuspeitoDesde, (DateTimeOffset?)null), Ct);
    }

    public Task<long> PendenciasNoRedisAsync() => fixture.Redis.GetDatabase().StreamLengthAsync(ChavesExtrato.Pendencias);
}
