using EuroBank.Extrato.Infrastructure.Cache.TransferObjects;
using EuroBank.Extrato.Infrastructure.Enums;
using EuroBank.Extrato.Infrastructure.Mappers;
using EuroBank.Extrato.Infrastructure.Persistence;
using EuroBank.Shared.Domain;
using Microsoft.Extensions.Logging;

namespace EuroBank.Extrato.Infrastructure.Cache.WriteBehind;

/// <summary>
/// No write-behind, cada extrato alterado vai para o Redis num script só: o resumo, os lançamentos novos e a pendência
/// para o Postgres. Se o Redis falhar na hora, o cache vira suspeito e a pendência é gravada direto no Postgres: o evento
/// não se perde e o Redis, que pode ter ficado para trás, deixa de ser lido até ser reconstruído.
/// </summary>
public sealed class UnidadeDeTrabalhoWriteBehind(
    UnidadeDeTrabalho postgres,
    ExtratoDbContext contexto,
    SessaoWriteBehind sessao,
    CacheExtratos cache,
    GravadorExtratosPostgres gravador,
    MarcaCacheSuspeito marca,
    MetricasCache metricas,
    ILogger<UnidadeDeTrabalhoWriteBehind> logger) : IUnidadeDeTrabalho
{
    public async Task SalvarAsync(CancellationToken cancellationToken)
    {
        if (contexto.ChangeTracker.HasChanges())
        {
            await postgres.SalvarAsync(cancellationToken);
        }

        foreach (var extrato in sessao.Alterados())
        {
            var estado = EstadoExtrato.De(extrato);
            var pendencia = new PendenciaExtrato(estado, extrato.Lancamentos.Select(LancamentoGravado.De).ToList());
            try
            {
                var resultado = await cache.AplicarAsync(
                    estado, extrato.Lancamentos.Select(LancamentoMapeador.ParaDto).ToList(), exigirExistente: false, ttl: null, pendencia);
                logger.LogDebug("Write-behind do extrato {ContaId} na versão {Versao}: {Resultado}", estado.ContaId, estado.Versao, resultado);
            }
            catch (Exception erro) when (FalhaRedis.Eh(erro))
            {
                metricas.Falha(EstrategiaCache.WriteBehind, "escrita");
                await marca.MarcarAsync(erro, cancellationToken);
                await gravador.GravarAsync([pendencia], cancellationToken);
            }

            sessao.Confirmar(extrato);
        }
    }
}
