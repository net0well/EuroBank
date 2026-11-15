using System.Diagnostics;
using EuroBank.Extrato.Application.Interfaces;
using EuroBank.Extrato.Application.TransferObjects;
using EuroBank.Extrato.Infrastructure.Enums;
using EuroBank.Extrato.Infrastructure.Options;
using EuroBank.Extrato.Infrastructure.Persistence.Queries;
using EuroBank.Shared.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EuroBank.Extrato.Infrastructure.Cache;

/// <summary>
/// A leitura do extrato com o Redis na frente. A página que cabe nos lançamentos guardados vem do Redis; a que não
/// acha a conta lê o retrato do Postgres e o grava (com TTL no cache-aside). Páginas além do que o cache guarda, cache
/// suspeito ou Redis fora: Postgres.
/// </summary>
public sealed class ConsultaExtratosComCache(
    ConsultaExtratos postgres,
    CacheExtratos cache,
    MarcaCacheSuspeito marca,
    MetricasCache metricas,
    IOptions<OpcoesCache> opcoes,
    TimeProvider relogio,
    ILogger<ConsultaExtratosComCache> logger) : IConsultaExtratos
{
    private EstrategiaCache Estrategia => opcoes.Value.Estrategia;

    public async Task<ExtratoDto?> ObterAsync(Guid contaId, Paginacao paginacao, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(paginacao);
        var inicio = Stopwatch.GetTimestamp();
        if (paginacao.Saltar + paginacao.Tamanho > OpcoesCache.LancamentosNoCache || await marca.EstaSuspeitoAsync(cancellationToken))
        {
            return await DoPostgresAsync(contaId, paginacao, inicio, cancellationToken);
        }

        try
        {
            if (await cache.ObterAsync(contaId, OpcoesCache.LancamentosNoCache) is { } guardado
                && guardado.ParaDto(paginacao) is { } doCache)
            {
                metricas.Acerto(Estrategia, relogio.GetUtcNow() - guardado.GravadoEm!.Value);
                metricas.Leitura(Estrategia, "redis", Stopwatch.GetElapsedTime(inicio));
                return doCache;
            }
        }
        catch (Exception erro) when (FalhaRedis.Eh(erro))
        {
            metricas.Falha(Estrategia, "leitura");
            logger.LogWarning(erro, "Redis indisponível na leitura do extrato {ContaId}; lendo do Postgres", contaId);
            return await DoPostgresAsync(contaId, paginacao, inicio, cancellationToken);
        }

        metricas.Falta(Estrategia);
        var retrato = await postgres.ObterRetratoAsync(contaId, OpcoesCache.LancamentosNoCache, cancellationToken);
        if (retrato is null)
        {
            return null;
        }

        try
        {
            await cache.GravarRetratoAsync(retrato, Estrategia == EstrategiaCache.CacheAside ? opcoes.Value.TtlCacheAside : null);
        }
        catch (Exception erro) when (FalhaRedis.Eh(erro))
        {
            // Não gravar o retrato não deixa dado velho no Redis: a próxima leitura tenta de novo.
            metricas.Falha(Estrategia, "retrato");
            logger.LogWarning(erro, "Redis indisponível ao gravar o retrato do extrato {ContaId}", contaId);
        }

        metricas.Leitura(Estrategia, "postgres", Stopwatch.GetElapsedTime(inicio));
        return retrato.ParaDto(paginacao) ?? await postgres.ObterAsync(contaId, paginacao, cancellationToken);
    }

    private async Task<ExtratoDto?> DoPostgresAsync(Guid contaId, Paginacao paginacao, long inicio, CancellationToken cancellationToken)
    {
        var extrato = await postgres.ObterAsync(contaId, paginacao, cancellationToken);
        metricas.Leitura(Estrategia, "postgres", Stopwatch.GetElapsedTime(inicio));
        return extrato;
    }
}
