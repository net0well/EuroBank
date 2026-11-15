using EuroBank.Extrato.Domain.Entities;
using EuroBank.Extrato.Domain.Repositories;
using EuroBank.Extrato.Infrastructure.Options;
using EuroBank.Extrato.Infrastructure.Persistence.Queries;
using EuroBank.Extrato.Infrastructure.Persistence.Repositories;

namespace EuroBank.Extrato.Infrastructure.Cache.WriteBehind;

/// <summary>
/// No write-behind, o extrato vive no Redis: o agregado é reconstituído do resumo guardado lá. Conta que ainda não
/// está no Redis tem o retrato lido do Postgres e gravado antes, para a primeira página do cache sair completa. Com o
/// cache suspeito ou o Redis fora, a mensagem inteira usa o Postgres, como sem cache.
/// </summary>
public sealed class RepositorioExtratosWriteBehind(
    RepositorioExtratos postgres,
    ConsultaExtratos consulta,
    CacheExtratos cache,
    MarcaCacheSuspeito marca,
    SessaoWriteBehind sessao) : IRepositorioExtratos
{
    public async Task<ExtratoConta?> ObterAsync(Guid contaId, CancellationToken cancellationToken)
    {
        sessao.UsaPostgres ??= await marca.EstaSuspeitoAsync(cancellationToken);
        if (sessao.UsaPostgres.Value)
        {
            return await postgres.ObterAsync(contaId, cancellationToken);
        }

        try
        {
            var estado = await cache.ObterEstadoAsync(contaId);
            if (estado is null)
            {
                var retrato = await consulta.ObterRetratoAsync(contaId, OpcoesCache.LancamentosNoCache, cancellationToken);
                if (retrato is null)
                {
                    return null;
                }

                await cache.GravarRetratoAsync(retrato, ttl: null);
                estado = retrato.Estado;
            }

            var extrato = estado.ParaAgregado();
            sessao.Rastrear(extrato, estado.Versao);
            return extrato;
        }
        catch (Exception erro) when (FalhaRedis.Eh(erro))
        {
            await marca.MarcarAsync(erro, cancellationToken);
            sessao.UsaPostgres = true;
            return await postgres.ObterAsync(contaId, cancellationToken);
        }
    }

    public void Adicionar(ExtratoConta extrato)
    {
        ArgumentNullException.ThrowIfNull(extrato);
        if (sessao.UsaPostgres == true)
        {
            postgres.Adicionar(extrato);
            return;
        }

        sessao.Rastrear(extrato, versaoCarregada: 0);
    }
}
