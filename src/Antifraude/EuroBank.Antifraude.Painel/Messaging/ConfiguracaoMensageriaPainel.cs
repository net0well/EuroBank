using Confluent.Kafka;
using EuroBank.Antifraude.Painel.Handlers;
using EuroBank.Antifraude.Painel.TransferObjects;
using EuroBank.Shared.Messaging;
using Wolverine;
using Wolverine.Kafka;
using Wolverine.Runtime;

namespace EuroBank.Antifraude.Painel.Messaging;

public static class ConfiguracaoMensageriaPainel
{
    public const string Topico = "eurobank.antifraude.eventos";

    /// <summary>Os tipos publicados no tópico que o painel lê.</summary>
    public static IReadOnlyDictionary<string, Type> Tipos { get; } = new Dictionary<string, Type>(StringComparer.Ordinal)
    {
        ["CasoAberto"] = typeof(EventoCasoAberto),
        ["AnaliseDecidida"] = typeof(EventoAnaliseDecidida),
    };

    /// <summary>
    /// Wolverine só para ouvir eurobank.antifraude.eventos, sem banco: o evento apenas avisa as telas, então não há inbox
    /// nem dead letter. Cada instância tem o próprio consumer group (o grupo configurado mais o nome da máquina), para
    /// receber todos os eventos, e começa do fim: um painel que sobe não quer a história, só o que acontecer agora.
    /// </summary>
    public static WolverineOptions ConfigurarMensageriaPainel(this WolverineOptions opcoes, IConfiguration configuracao)
    {
        var kafka = configuracao.GetSection(OpcoesKafka.Secao).Get<OpcoesKafka>() ?? new OpcoesKafka();
        var grupo = $"{(string.IsNullOrWhiteSpace(kafka.GrupoConsumidor) ? "eurobank-antifraude-painel" : kafka.GrupoConsumidor)}-{Environment.MachineName}";

        opcoes.UsarConvencoesEuroBank(typeof(EventosAntifraudeManipulador).Assembly);
        opcoes.Durability.Mode = DurabilityMode.Solo;
        opcoes.UseKafka(kafka.BootstrapServers)
            .ConfigureConsumers(consumidor =>
            {
                consumidor.GroupId = grupo;
                consumidor.AutoOffsetReset = AutoOffsetReset.Latest;
            });
        opcoes.ListenToKafkaTopic(Topico)
            .UseInterop((IWolverineRuntime _, KafkaTopic _) => new MapeadorEnvelopeDebezium(Tipos))
            .BufferedInMemory();
        return opcoes;
    }
}
