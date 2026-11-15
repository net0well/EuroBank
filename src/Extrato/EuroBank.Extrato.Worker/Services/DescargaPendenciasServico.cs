using EuroBank.Extrato.Infrastructure.Cache;
using EuroBank.Extrato.Infrastructure.Cache.WriteBehind;
using EuroBank.Extrato.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace EuroBank.Extrato.Worker.Services;

/// <summary>
/// Descarrega as pendências do write-behind a cada Cache:IntervaloDescarga. Roda sempre que há Redis configurado, mesmo
/// com outra estratégia, para esvaziar o que ficou de antes de uma troca.
/// </summary>
public sealed class DescargaPendenciasServico(
    DescargaPendencias descarga,
    IConfiguration configuracao,
    IOptions<OpcoesCache> opcoes,
    TimeProvider relogio,
    ILogger<DescargaPendenciasServico> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(configuracao.GetConnectionString("Redis")))
        {
            return;
        }

        using var temporizador = new PeriodicTimer(opcoes.Value.IntervaloDescarga, relogio);
        while (await temporizador.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await descarga.DescarregarAsync(stoppingToken);
            }
            catch (Exception erro) when (FalhaRedis.Eh(erro))
            {
                logger.LogDebug(erro, "Redis indisponível: a descarga das pendências fica para o próximo ciclo");
            }
            catch (Exception erro) when (erro is not OperationCanceledException)
            {
                logger.LogError(erro, "Falha ao descarregar as pendências do write-behind; nova tentativa no próximo ciclo");
            }
        }
    }
}
