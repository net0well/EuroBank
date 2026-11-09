using System.Diagnostics;
using EuroBank.Simuladores.BancoExterno.Persistencia;
using EuroBank.Simuladores.Comum;

namespace EuroBank.Simuladores.BancoExterno.Servicos;

/// <summary>
/// Publica as respostas agendadas quando chega a hora. É o que dá ao simulador latência, atrasos e respostas
/// tardias sem segurar a thread do consumidor nem perder nada num restart (a agenda está no banco).
/// </summary>
public sealed class EntregadorRespostas(
    RepositorioBancoExterno repositorio,
    PublicadorKafka publicador,
    TimeProvider relogio,
    ILogger<EntregadorRespostas> logger) : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromMilliseconds(500);

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
        using var atividade = Rastreamento.Iniciar(
            Telemetria.Fonte, $"banco-externo publicar {resposta.Tipo}", ActivityKind.Producer, resposta.Traceparent);

        await publicador.PublicarAsync(
            Topicos.Respostas,
            resposta.TransferenciaId.ToString(),
            resposta.Tipo,
            resposta.Payload,
            atividade?.Id ?? resposta.Traceparent,
            cancellationToken);

        logger.LogInformation("{Tipo} da transferência {TransferenciaId} publicada", resposta.Tipo, resposta.TransferenciaId);
    }
}
