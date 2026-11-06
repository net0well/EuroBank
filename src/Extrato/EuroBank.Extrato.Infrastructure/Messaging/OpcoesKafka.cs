namespace EuroBank.Extrato.Infrastructure.Messaging;

/// <summary>Seção "Kafka" do appsettings.</summary>
public sealed class OpcoesKafka
{
    public const string Secao = "Kafka";

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string Topico { get; set; } = "eurobank.contas.eventos";

    /// <summary>Consumer group: instâncias com o mesmo grupo dividem as partições entre si.</summary>
    public string GrupoConsumidor { get; set; } = "eurobank-extrato";
}
