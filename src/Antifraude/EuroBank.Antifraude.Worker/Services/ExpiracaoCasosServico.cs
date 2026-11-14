using EuroBank.Antifraude.Application.Commands;
using EuroBank.Antifraude.Domain.Repositories;
using EuroBank.Antifraude.Worker.Options;
using Microsoft.Extensions.Options;
using Wolverine;

namespace EuroBank.Antifraude.Worker.Services;

/// <summary>
/// A cada intervalo, expira os casos que passaram do prazo, cada um no próprio comando (DbContext e transação novos):
/// um conflito com a decisão do analista num caso não atrapalha os outros.
/// </summary>
public sealed class ExpiracaoCasosServico(
    IServiceScopeFactory escopos,
    IOptions<OpcoesExpiracao> opcoes,
    TimeProvider relogio,
    ILogger<ExpiracaoCasosServico> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var temporizador = new PeriodicTimer(opcoes.Value.Intervalo, relogio);
        while (await temporizador.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ExpirarVencidosAsync(stoppingToken);
            }
            catch (Exception erro) when (erro is not OperationCanceledException)
            {
                logger.LogError(erro, "Falha na expiração dos casos; nova tentativa no próximo ciclo");
            }
        }
    }

    /// <summary>Um ciclo da varredura (público para os testes chamarem sem esperar o temporizador).</summary>
    public async Task<int> ExpirarVencidosAsync(CancellationToken cancellationToken)
    {
        List<Guid> vencidos;
        await using (var escopo = escopos.CreateAsyncScope())
        {
            vencidos = (await escopo.ServiceProvider.GetRequiredService<IRepositorioCasos>()
                    .VencidosAsync(relogio.GetUtcNow(), opcoes.Value.Lote, cancellationToken))
                .Select(caso => caso.Id.Valor)
                .ToList();
        }

        foreach (var casoId in vencidos)
        {
            try
            {
                await using var escopo = escopos.CreateAsyncScope();
                await escopo.ServiceProvider.GetRequiredService<IMessageBus>().InvokeAsync(new ExpirarCaso(casoId), cancellationToken);
            }
            catch (Exception erro) when (erro is not OperationCanceledException)
            {
                logger.LogWarning(erro, "Caso {CasoId} não expirou neste ciclo", casoId);
            }
        }

        return vencidos.Count;
    }
}
