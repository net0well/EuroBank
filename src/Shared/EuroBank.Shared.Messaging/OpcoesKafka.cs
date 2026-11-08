namespace EuroBank.Shared.Messaging;

/// <summary>Seção "Kafka" do appsettings de um consumidor.</summary>
public sealed class OpcoesKafka
{
    public const string Secao = "Kafka";

    public string BootstrapServers { get; set; } = "localhost:9092";

    /// <summary>Consumer group: instâncias com o mesmo grupo dividem as partições entre si.</summary>
    public string GrupoConsumidor { get; set; } = string.Empty;
}
