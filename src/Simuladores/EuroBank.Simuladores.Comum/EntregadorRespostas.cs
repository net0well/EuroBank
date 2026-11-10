using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EuroBank.Simuladores.Comum;

/// <summary>
/// Publica as respostas agendadas de um simulador quando chega a hora. É o que dá latência, atrasos e respostas
/// tardias sem segurar a thread do consumidor nem perder nada num restart (a agenda está no banco).
/// </summary>
public sealed class EntregadorRespostas(
    RepositorioSimulador repositorio,
    PublicadorKafka publicador,
    TimeProvider relogio,
    ActivitySource fonte,
    string topico,
    ILogger<EntregadorRespostas> logger) : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromMilliseconds(200);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var temporizador = new PeriodicTimer(Intervalo, relogio);
        while (await temporizador.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await repositorio.EntregarVencidasAsync(
                    relogio.GetUtcNow(), resposta => PublicarAsync(resposta, stoppingToken), stoppingToken);
            }
            catch (Exception erro) when (erro is not OperationCanceledException)
            {
                logger.LogError(erro, "Falha ao entregar respostas agendadas; nova tentativa no próximo ciclo");
            }
        }
    }

    private async Task PublicarAsync(RespostaAgendada resposta, CancellationToken cancellationToken)
    {
        using var atividade = Rastreamento.Iniciar(fonte, $"publicar {resposta.Tipo}", ActivityKind.Producer, resposta.Traceparent);

        await publicador.PublicarAsync(
            topico,
            resposta.TransferenciaId.ToString(),
            resposta.Tipo,
            resposta.Payload,
            atividade?.Id ?? resposta.Traceparent,
            cancellationToken);

        logger.LogInformation("{Tipo} da transferência {TransferenciaId} publicada em {Topico}",
            resposta.Tipo, resposta.TransferenciaId, topico);
    }
}
