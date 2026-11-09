using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EuroBank.Simuladores.Comum;

/// <summary>
/// Laço de consumo escrito à mão com Confluent.Kafka, sem framework: lê, processa, confirma o offset. Mostra o que o
/// Wolverine faz por baixo nos serviços do EuroBank. Entrega "pelo menos uma vez": quem herda precisa ser idempotente.
/// </summary>
public abstract class ConsumidorKafka(OpcoesKafkaSimulador opcoes, string topico, ILogger logger) : BackgroundService
{
    private const int Tentativas = 3;

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        // Consume() bloqueia a thread: roda fora do contexto de inicialização do host.
        Task.Run(() => ConsumirAsync(stoppingToken), stoppingToken);

    protected abstract Task ProcessarAsync(MensagemRecebida mensagem, CancellationToken cancellationToken);

    private async Task ConsumirAsync(CancellationToken cancellationToken)
    {
        var configuracao = new ConsumerConfig
        {
            BootstrapServers = opcoes.BootstrapServers,
            GroupId = opcoes.GrupoConsumidor,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            // Offset confirmado à mão, só depois de processar a mensagem.
            EnableAutoCommit = false,
        };

        using var consumidor = new ConsumerBuilder<string, string>(configuracao).Build();
        consumidor.Subscribe(topico);
        logger.LogInformation("Consumindo {Topico} no grupo {Grupo}", topico, opcoes.GrupoConsumidor);

        while (!cancellationToken.IsCancellationRequested)
        {
            ConsumeResult<string, string> resultado;
            try
            {
                resultado = consumidor.Consume(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ConsumeException erro)
            {
                logger.LogWarning(erro, "Falha ao ler de {Topico}; tentando de novo", topico);
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                continue;
            }

            await ProcessarComRetryAsync(Ler(resultado), cancellationToken);
            consumidor.Commit(resultado);
        }

        consumidor.Close();
    }

    /// <summary>Algumas tentativas para falhas transitórias; depois registra o erro e segue (sem dead letter).</summary>
    private async Task ProcessarComRetryAsync(MensagemRecebida mensagem, CancellationToken cancellationToken)
    {
        for (var tentativa = 1; tentativa <= Tentativas; tentativa++)
        {
            try
            {
                await ProcessarAsync(mensagem, cancellationToken);
                return;
            }
            catch (Exception erro) when (erro is not OperationCanceledException)
            {
                logger.LogError(erro, "Falha ao processar {Tipo} {Id} (tentativa {Tentativa} de {Total})",
                    mensagem.Tipo, mensagem.Id, tentativa, Tentativas);
                if (tentativa < Tentativas)
                {
                    await Task.Delay(TimeSpan.FromSeconds(tentativa), cancellationToken);
                }
            }
        }
    }

    private static MensagemRecebida Ler(ConsumeResult<string, string> resultado) => new(
        Header(resultado, "id"),
        Header(resultado, "tipo"),
        resultado.Message.Key,
        resultado.Message.Value,
        Header(resultado, "traceparent"));

    private static string? Header(ConsumeResult<string, string> resultado, string nome) =>
        resultado.Message.Headers is not null
        && resultado.Message.Headers.TryGetLastBytes(nome, out var bytes)
        && bytes is not null
            ? Encoding.UTF8.GetString(bytes)
            : null;
}
