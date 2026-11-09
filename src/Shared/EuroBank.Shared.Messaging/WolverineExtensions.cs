using System.Reflection;
using System.Text.Json.Serialization;
using Confluent.Kafka;
using EuroBank.Shared.Exceptions;
using JasperFx.CodeGeneration.Model;
using Wolverine;
using Wolverine.Configuration;
using Wolverine.ErrorHandling;
using Wolverine.Kafka;
using Wolverine.Postgresql;
using Wolverine.Runtime;

namespace EuroBank.Shared.Messaging;

/// <summary>Configuração do Wolverine comum aos consumidores do EuroBank.</summary>
public static class WolverineExtensions
{
    /// <summary>Período em que a inbox durável lembra das mensagens já processadas (deduplicação pelo id).</summary>
    public static readonly TimeSpan RetencaoInbox = TimeSpan.FromDays(7);

    /// <summary>
    /// Descobre os manipuladores (classes *Manipulador com método Handle) do assembly da camada Application e
    /// lê JSON em camelCase com valores monetários como string ("100.00").
    /// </summary>
    public static WolverineOptions UsarConvencoesEuroBank(this WolverineOptions opcoes, Assembly assemblyManipuladores)
    {
        opcoes.Discovery.IncludeAssembly(assemblyManipuladores);
        opcoes.Discovery.CustomizeHandlerDiscovery(consulta => consulta.Includes.WithNameSuffix("Manipulador"));

        // O DbContext é registrado por fábrica (AddDbContext); o código gerado resolve pelo container do escopo.
        opcoes.ServiceLocationPolicy = ServiceLocationPolicy.AlwaysAllowed;

        opcoes.UseSystemTextJsonForSerialization(json =>
        {
            json.PropertyNameCaseInsensitive = true;
            json.NumberHandling = JsonNumberHandling.AllowReadingFromString;
        });

        return opcoes;
    }

    /// <summary>Inbox durável e dead letter (wolverine_dead_letters) no banco do contexto, schema "wolverine".</summary>
    public static WolverineOptions UsarInboxDuravel(this WolverineOptions opcoes, string connectionString)
    {
        opcoes.PersistMessagesWithPostgresql(connectionString, "wolverine");
        opcoes.Durability.KeepAfterMessageHandling = RetencaoInbox;

        // A verificação periódica de nós do Wolverine geraria um trace a cada poucos segundos no Jaeger.
        opcoes.Durability.NodeAssignmentHealthCheckTracingEnabled = false;
        return opcoes;
    }

    public static KafkaTransportExpression UsarKafka(this WolverineOptions opcoes, OpcoesKafka kafka) =>
        opcoes.UseKafka(kafka.BootstrapServers)
            .ConfigureConsumers(consumidor =>
            {
                consumidor.GroupId = kafka.GrupoConsumidor;
                consumidor.AutoOffsetReset = AutoOffsetReset.Earliest;
            });

    /// <summary>
    /// Ouve um tópico alimentado pelo Debezium. Mensagens com a mesma chave ficam estritamente em ordem e chaves
    /// diferentes processam em paralelo; isso já liga a inbox durável (o offset só é confirmado depois de a
    /// mensagem ir para a inbox).
    /// </summary>
    public static KafkaListenerConfiguration OuvirTopicoDebezium(
        this WolverineOptions opcoes,
        string topico,
        IReadOnlyDictionary<string, Type> tiposPorEvento) =>
        opcoes.ListenToKafkaTopic(topico)
            .UseInterop((IWolverineRuntime _, KafkaTopic _) => new MapeadorEnvelopeDebezium(tiposPorEvento))
            .ProcessConcurrentlyByKey(PartitionSlots.Five);

    /// <summary>
    /// Política de erros, da regra mais específica para a mais geral:
    /// 1. conflito de concorrência é transitório: tenta de novo;
    /// 2. erro de domínio ou mensagem inválida não melhora com retry: dead letter na hora;
    /// 3. qualquer outra falha (banco fora, timeout): 3 tentativas com espera crescente, depois dead letter.
    /// </summary>
    public static WolverineOptions AplicarPoliticaDeErros(this WolverineOptions opcoes)
    {
        opcoes.OnException<ExcecaoConcorrencia>()
            .RetryWithCooldown(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(500))
            .Then.MoveToErrorQueue();
        opcoes.OnException<ExcecaoDominio>().MoveToErrorQueue();
        opcoes.OnException<Exception>()
            .RetryWithCooldown(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3))
            .Then.MoveToErrorQueue();
        return opcoes;
    }
}
