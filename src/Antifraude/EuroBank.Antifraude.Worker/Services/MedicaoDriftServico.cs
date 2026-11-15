using EuroBank.Antifraude.Application.Commands;
using EuroBank.Antifraude.Application.TransferObjects;
using EuroBank.Antifraude.Domain.ValueObjects;
using EuroBank.Antifraude.Worker.Observability;
using EuroBank.Antifraude.Worker.Options;
using Microsoft.Extensions.Options;
using Wolverine;

namespace EuroBank.Antifraude.Worker.Services;

/// <summary>
/// A cada intervalo, mede o drift da versão ativa (PSI das features e do score nas análises da janela contra o treino),
/// grava a medição e atualiza as métricas. Uma falha num ciclo vai para o log; o próximo tenta de novo.
/// </summary>
public sealed class MedicaoDriftServico(
    IServiceScopeFactory escopos,
    IOptions<OpcoesDrift> opcoes,
    TelemetriaDrift telemetria,
    TimeProvider relogio,
    ILogger<MedicaoDriftServico> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var temporizador = new PeriodicTimer(opcoes.Value.Intervalo, relogio);
        while (await temporizador.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await MedirAsync(relogio.GetUtcNow(), stoppingToken);
            }
            catch (Exception erro) when (erro is not OperationCanceledException)
            {
                logger.LogError(erro, "Falha na medição de drift; nova tentativa no próximo ciclo");
            }
        }
    }

    /// <summary>Um ciclo (público para os testes chamarem com o momento que quiserem, sem esperar o temporizador).</summary>
    public async Task<MedicaoDriftDto?> MedirAsync(DateTimeOffset agora, CancellationToken cancellationToken)
    {
        var valores = opcoes.Value;
        var comando = new MedirDrift(
            agora,
            valores.Janela,
            new LimitesDrift(valores.AmostraMinima, valores.LimiarModerado, valores.LimiarSignificativo),
            valores.AmostraMaxima,
            valores.Retencao);

        await using var escopo = escopos.CreateAsyncScope();
        var resultado = await escopo.ServiceProvider.GetRequiredService<IMessageBus>().InvokeAsync<ResultadoMedicaoDrift>(comando, cancellationToken);
        telemetria.Registrar(resultado.Medicao);
        return resultado.Medicao;
    }
}
