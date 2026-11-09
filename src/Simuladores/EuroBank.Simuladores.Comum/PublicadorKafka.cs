using System.Text;
using Confluent.Kafka;

namespace EuroBank.Simuladores.Comum;

/// <summary>
/// Produtor idempotente (sem duplicatas causadas por retry do próprio produtor) que publica com os mesmos headers
/// que o Debezium usa: id, tipo e traceparent. Assim o EuroBank consome as respostas como qualquer outra mensagem.
/// </summary>
public sealed class PublicadorKafka : IDisposable
{
    private readonly IProducer<string, string> _produtor;

    public PublicadorKafka(OpcoesKafkaSimulador opcoes) =>
        _produtor = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = opcoes.BootstrapServers,
            EnableIdempotence = true,
            Acks = Acks.All,
        }).Build();

    public async Task PublicarAsync(
        string topico,
        string chave,
        string tipo,
        string payload,
        string? traceparent,
        CancellationToken cancellationToken)
    {
        var headers = new Headers
        {
            { "id", Encoding.UTF8.GetBytes(Guid.CreateVersion7().ToString()) },
            { "tipo", Encoding.UTF8.GetBytes(tipo) },
        };
        if (traceparent is not null)
        {
            headers.Add("traceparent", Encoding.UTF8.GetBytes(traceparent));
        }

        await _produtor.ProduceAsync(
            topico, new Message<string, string> { Key = chave, Value = payload, Headers = headers }, cancellationToken);
    }

    public void Dispose()
    {
        _produtor.Flush(TimeSpan.FromSeconds(5));
        _produtor.Dispose();
    }
}
