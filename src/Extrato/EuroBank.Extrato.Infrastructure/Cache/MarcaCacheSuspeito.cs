using EuroBank.Extrato.Infrastructure.Options;
using EuroBank.Extrato.Infrastructure.Persistence;
using EuroBank.Extrato.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EuroBank.Extrato.Infrastructure.Cache;

/// <summary>
/// A marca global de cache suspeito, no Postgres: uma escrita no Redis falhou, então ele pode ter dados velhos.
/// Enquanto ela existe, leituras e escritas usam só o Postgres; o worker a tira quando o Redis volta e as chaves do
/// extrato são apagadas. Cada instância confia na leitura por um instante, para não consultar a tabela a cada pedido.
/// </summary>
public sealed class MarcaCacheSuspeito(
    IServiceScopeFactory escopos,
    TimeProvider relogio,
    IOptions<OpcoesCache> opcoes,
    ILogger<MarcaCacheSuspeito> logger)
{
    private readonly Lock _trava = new();
    private bool _suspeito;
    private DateTimeOffset _lidaEm = DateTimeOffset.MinValue;

    /// <param name="releitura">Ignora a leitura guardada e consulta o Postgres.</param>
    public async ValueTask<bool> EstaSuspeitoAsync(CancellationToken cancellationToken, bool releitura = false)
    {
        var agora = relogio.GetUtcNow();
        lock (_trava)
        {
            if (!releitura && agora - _lidaEm < opcoes.Value.ValidadeLeituraMarca)
            {
                return _suspeito;
            }
        }

        await using var escopo = escopos.CreateAsyncScope();
        var desde = await escopo.ServiceProvider.GetRequiredService<ExtratoDbContext>().MarcasCache
            .AsNoTracking()
            .Where(marca => marca.Id == MarcaCache.IdUnico)
            .Select(marca => marca.SuspeitoDesde)
            .SingleOrDefaultAsync(cancellationToken);
        Guardar(desde is not null, agora);
        return desde is not null;
    }

    public async Task MarcarAsync(Exception causa, CancellationToken cancellationToken)
    {
        var agora = relogio.GetUtcNow();
        Guardar(suspeito: true, agora);
        await using var escopo = escopos.CreateAsyncScope();
        var marcadas = await escopo.ServiceProvider.GetRequiredService<ExtratoDbContext>().MarcasCache
            .Where(marca => marca.Id == MarcaCache.IdUnico && marca.SuspeitoDesde == null)
            .ExecuteUpdateAsync(marca => marca.SetProperty(m => m.SuspeitoDesde, agora), cancellationToken);
        if (marcadas > 0)
        {
            logger.LogWarning(causa, "Escrita no Redis falhou: cache do extrato marcado como suspeito; leituras vão ao Postgres");
        }
    }

    public async Task LimparAsync(CancellationToken cancellationToken)
    {
        await using var escopo = escopos.CreateAsyncScope();
        await escopo.ServiceProvider.GetRequiredService<ExtratoDbContext>().MarcasCache
            .Where(marca => marca.Id == MarcaCache.IdUnico)
            .ExecuteUpdateAsync(marca => marca.SetProperty(m => m.SuspeitoDesde, (DateTimeOffset?)null), cancellationToken);
        Guardar(suspeito: false, relogio.GetUtcNow());
    }

    private void Guardar(bool suspeito, DateTimeOffset agora)
    {
        lock (_trava)
        {
            _suspeito = suspeito;
            _lidaEm = agora;
        }
    }
}
