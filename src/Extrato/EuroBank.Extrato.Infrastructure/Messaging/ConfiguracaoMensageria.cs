using System.Text.Json.Serialization;
using Confluent.Kafka;
using EuroBank.Extrato.Application.Handlers;
using EuroBank.Shared.Exceptions;
using JasperFx.CodeGeneration.Model;
using Microsoft.Extensions.Configuration;
using Wolverine;
using Wolverine.Configuration;
using Wolverine.ErrorHandling;
using Wolverine.Kafka;
using Wolverine.Postgresql;
using Wolverine.Runtime;

namespace EuroBank.Extrato.Infrastructure.Messaging;

public static class ConfiguracaoMensageria
{
    /// <summary>Período em que a inbox durável lembra das mensagens já processadas (deduplicação pelo id).</summary>
    public static readonly TimeSpan RetencaoInbox = TimeSpan.FromDays(7);

    /// <summary>
    /// Wolverine no Extrato: consome eurobank.contas.eventos com inbox durável no Postgres, retry para falhas
    /// transitórias e dead letter para o que não tem como dar certo.
    /// </summary>
    public static WolverineOptions ConfigurarMensageriaExtrato(this WolverineOptions opcoes, IConfiguration configuracao)
    {
        var kafka = configuracao.GetSection(OpcoesKafka.Secao).Get<OpcoesKafka>() ?? new OpcoesKafka();
        var connectionString = configuracao.GetConnectionString("Extrato")
            ?? throw new InvalidOperationException("ConnectionStrings:Extrato não configurada.");

        opcoes.Discovery.IncludeAssembly(typeof(ContaAbertaManipulador).Assembly);
        opcoes.Discovery.CustomizeHandlerDiscovery(consulta => consulta.Includes.WithNameSuffix("Manipulador"));

        // O DbContext é registrado por fábrica (AddDbContext); o código gerado resolve pelo container do escopo.
        opcoes.ServiceLocationPolicy = ServiceLocationPolicy.AlwaysAllowed;

        // O corpo vem em camelCase e com valores monetários como string ("100.00").
        opcoes.UseSystemTextJsonForSerialization(json =>
        {
            json.PropertyNameCaseInsensitive = true;
            json.NumberHandling = JsonNumberHandling.AllowReadingFromString;
        });

        // Inbox durável e dead letter (wolverine_dead_letters) no database do Extrato, schema "wolverine".
        opcoes.PersistMessagesWithPostgresql(connectionString, "wolverine");
        opcoes.Durability.KeepAfterMessageHandling = RetencaoInbox;

        // A verificação periódica de nós do Wolverine geraria um trace a cada poucos segundos no Jaeger.
        opcoes.Durability.NodeAssignmentHealthCheckTracingEnabled = false;

        opcoes.UseKafka(kafka.BootstrapServers)
            .ConfigureConsumers(consumidor =>
            {
                consumidor.GroupId = kafka.GrupoConsumidor;
                consumidor.AutoOffsetReset = AutoOffsetReset.Earliest;
            });

        // Mensagens da mesma conta (mesma chave) ficam estritamente em ordem; contas diferentes processam em
        // paralelo. Isso já liga a inbox durável: o offset só é confirmado depois de a mensagem ir para a inbox.
        opcoes.ListenToKafkaTopic(kafka.Topico)
            .UseInterop((IWolverineRuntime _, KafkaTopic _) => new MapeadorEnvelopeDebezium())
            .ProcessConcurrentlyByKey(PartitionSlots.Five);

        // Política de erros, da regra mais específica para a mais geral:
        // 1. conflito de concorrência é transitório: tenta de novo (a regra de versão resolve a duplicata);
        // 2. erro de domínio ou mensagem inválida não melhora com retry: dead letter na hora;
        // 3. qualquer outra falha (banco fora, timeout): 3 tentativas com espera crescente, depois dead letter.
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
