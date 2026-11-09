using EuroBank.Transferencias.Application.Services;
using EuroBank.Transferencias.Domain.ValueObjects;
using EuroBank.Transferencias.Worker.Options;
using Microsoft.Extensions.Options;

namespace EuroBank.Transferencias.Worker.Services;

/// <summary>
/// Roda a varredura a cada intervalo. Cada transferência avança num escopo próprio (DbContext e transação
/// novos): uma falha ou um conflito numa delas não atrapalha as outras e é tentado de novo no próximo ciclo.
/// </summary>
public sealed class VarreduraSagasServico(
    IServiceScopeFactory escopos,
    IOptions<OpcoesVarredura> opcoes,
    TimeProvider relogio,
    ILogger<VarreduraSagasServico> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var temporizador = new PeriodicTimer(opcoes.Value.Intervalo, relogio);
        while (await temporizador.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ExecutarCicloAsync(stoppingToken);
            }
            catch (Exception erro) when (erro is not OperationCanceledException)
            {
                logger.LogError(erro, "Falha na varredura das sagas; nova tentativa no próximo ciclo");
            }
        }
    }

    private async Task ExecutarCicloAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<TransferenciaId> pendentes;
        await using (var escopo = escopos.CreateAsyncScope())
        {
            pendentes = await escopo.ServiceProvider.GetRequiredService<VarreduraTransferencias>()
                .ListarPendentesAsync(opcoes.Value.Lote, cancellationToken);
        }

        foreach (var id in pendentes)
        {
            try
            {
                await using var escopo = escopos.CreateAsyncScope();
                await escopo.ServiceProvider.GetRequiredService<VarreduraTransferencias>().AvancarAsync(id, cancellationToken);
            }
            catch (Exception erro) when (erro is not OperationCanceledException)
            {
                logger.LogWarning(erro, "Transferência {TransferenciaId} não avançou neste ciclo", id);
            }
        }
    }
}
