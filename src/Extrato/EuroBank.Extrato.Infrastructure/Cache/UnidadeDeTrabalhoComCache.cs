using EuroBank.Extrato.Domain.Entities;
using EuroBank.Extrato.Infrastructure.Cache.TransferObjects;
using EuroBank.Extrato.Infrastructure.Enums;
using EuroBank.Extrato.Infrastructure.Mappers;
using EuroBank.Extrato.Infrastructure.Options;
using EuroBank.Extrato.Infrastructure.Persistence;
using EuroBank.Extrato.Infrastructure.Persistence.Queries;
using EuroBank.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EuroBank.Extrato.Infrastructure.Cache;

/// <summary>
/// Grava no Postgres e, depois do commit, acerta o Redis: no cache-aside, apaga a entrada da conta; no write-through,
/// aplica o mesmo evento no cache (ou grava o retrato do Postgres, se a conta ainda não estava lá). Se o Redis falhar
/// aqui, ele pode ficar com dado velho: o cache é marcado como suspeito.
/// </summary>
public sealed class UnidadeDeTrabalhoComCache(
    ExtratoDbContext contexto,
    UnidadeDeTrabalho postgres,
    ConsultaExtratos consulta,
    CacheExtratos cache,
    MarcaCacheSuspeito marca,
    MetricasCache metricas,
    IOptions<OpcoesCache> opcoes,
    ILogger<UnidadeDeTrabalhoComCache> logger) : IUnidadeDeTrabalho
{
    public async Task SalvarAsync(CancellationToken cancellationToken)
    {
        // Antes do SaveChanges: depois dele as entradas ficam Unchanged. O repositório carrega o extrato sem o
        // histórico, então os lançamentos do agregado são só os deste evento.
        var alterados = contexto.ChangeTracker.Entries<ExtratoConta>()
            .Where(entrada => entrada.State is EntityState.Added or EntityState.Modified)
            .Select(entrada => (Estado: EstadoExtrato.De(entrada.Entity), Novos: entrada.Entity.Lancamentos.Select(LancamentoMapeador.ParaDto).ToList()))
            .ToList();

        await postgres.SalvarAsync(cancellationToken);

        if (alterados.Count == 0 || await marca.EstaSuspeitoAsync(cancellationToken))
        {
            return;
        }

        foreach (var (estado, novos) in alterados)
        {
            try
            {
                await AcertarCacheAsync(estado, novos, cancellationToken);
            }
            catch (Exception erro) when (FalhaRedis.Eh(erro))
            {
                metricas.Falha(opcoes.Value.Estrategia, "escrita");
                await marca.MarcarAsync(erro, cancellationToken);
                return;
            }
        }
    }

    private async Task AcertarCacheAsync(EstadoExtrato estado, IReadOnlyList<Application.TransferObjects.LancamentoDto> novos, CancellationToken cancellationToken)
    {
        if (opcoes.Value.Estrategia == EstrategiaCache.CacheAside)
        {
            await cache.RemoverAsync(estado.ContaId);
            return;
        }

        var resultado = await cache.AplicarAsync(estado, novos, exigirExistente: true, ttl: null);
        if (resultado == ResultadoEscritaCache.NaoExiste
            && await consulta.ObterRetratoAsync(estado.ContaId, OpcoesCache.LancamentosNoCache, cancellationToken) is { } retrato)
        {
            await cache.GravarRetratoAsync(retrato, ttl: null);
        }

        logger.LogDebug("Write-through do extrato {ContaId} na versão {Versao}: {Resultado}", estado.ContaId, estado.Versao, resultado);
    }
}
