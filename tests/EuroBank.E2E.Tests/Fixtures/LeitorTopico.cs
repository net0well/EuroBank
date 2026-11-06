using Confluent.Kafka;

namespace EuroBank.E2E.Tests.Fixtures;

/// <summary>Lê um tópico desde o início (consumer group próprio) até achar a mensagem procurada.</summary>
public static class LeitorTopico
{
    public static ConsumeResult<string, string> Encontrar(
        string bootstrap,
        string topico,
        Func<ConsumeResult<string, string>, bool> procurada,
        TimeSpan limite)
    {
        using var consumidor = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = bootstrap,
            GroupId = $"e2e-leitor-{Guid.NewGuid():N}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
        }).Build();

        consumidor.Subscribe(topico);
        var fim = DateTime.UtcNow + limite;
        while (DateTime.UtcNow < fim)
        {
            var resultado = consumidor.Consume(TimeSpan.FromSeconds(1));
            if (resultado?.Message is not null && procurada(resultado))
            {
                return resultado;
            }
        }

        throw new TimeoutException($"Nenhuma mensagem esperada chegou em {topico} em {limite.TotalSeconds:0}s.");
    }

    public static string? Header(this ConsumeResult<string, string> resultado, string nome) =>
        resultado.Message.Headers.TryGetLastBytes(nome, out var bytes) && bytes is not null
            ? System.Text.Encoding.UTF8.GetString(bytes)
            : null;
}
