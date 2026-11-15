using EuroBank.Extrato.Infrastructure.Cache;
using EuroBank.Extrato.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace EuroBank.Extrato.Worker.Services;

/// <summary>A cada Cache:IntervaloReconstrucao, confere se o cache suspeito já pode ser reconstruído.</summary>
public sealed class ReconstrucaoCacheServico(
    ReconstrucaoCache reconstrucao,
    IOptions<OpcoesCache> opcoes,
    TimeProvider relogio,
    ILogger<ReconstrucaoCacheServico> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!opcoes.Value.UsaRedis)
        {
            return;
        }

        using var temporizador = new PeriodicTimer(opcoes.Value.IntervaloReconstrucao, relogio);
        while (await temporizador.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await reconstrucao.TentarAsync(stoppingToken);
            }
            catch (Exception erro) when (erro is not OperationCanceledException)
            {
                logger.LogError(erro, "Falha ao reconstruir o cache do extrato; nova tentativa no próximo ciclo");
            }
        }
    }
}
