using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace EuroBank.Extrato.Infrastructure.Cache;

/// <summary>
/// Tira a marca de suspeito quando o Redis volta: apaga o resumo e os lançamentos de todas as contas (as leituras
/// seguintes repopulam pelo Postgres) e só então limpa a marca. Com pendências do write-behind ainda no stream, espera
/// a descarga terminar, para não montar o cache a partir de um Postgres atrasado.
/// </summary>
public sealed class ReconstrucaoCache(
    MarcaCacheSuspeito marca,
    CacheExtratos cache,
    IConnectionMultiplexer redis,
    ILogger<ReconstrucaoCache> logger)
{
    /// <summary>Um ciclo: verdadeiro se o cache estava suspeito e foi reconstruído agora.</summary>
    public async Task<bool> TentarAsync(CancellationToken cancellationToken)
    {
        if (!await marca.EstaSuspeitoAsync(cancellationToken, releitura: true))
        {
            return false;
        }

        try
        {
            await cache.PingAsync();
            var pendentes = await redis.GetDatabase().StreamLengthAsync(ChavesExtrato.Pendencias);
            if (pendentes > 0)
            {
                logger.LogInformation("Cache suspeito: esperando {Pendentes} pendências do write-behind chegarem ao Postgres", pendentes);
                return false;
            }

            var removidas = await cache.RemoverTodosAsync(cancellationToken);
            await marca.LimparAsync(cancellationToken);
            logger.LogInformation("Redis de volta: {Removidas} chaves do extrato apagadas e marca de suspeito retirada", removidas);
            return true;
        }
        catch (Exception erro) when (FalhaRedis.Eh(erro))
        {
            logger.LogDebug(erro, "Cache suspeito e Redis ainda indisponível");
            return false;
        }
    }
}
