namespace EuroBank.Simuladores.Comum;

/// <summary>Seção "Kafka" dos simuladores.</summary>
public sealed class OpcoesKafkaSimulador
{
    public const string Secao = "Kafka";

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string GrupoConsumidor { get; set; } = string.Empty;
}
