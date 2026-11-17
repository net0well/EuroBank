using EuroBank.Antifraude.Application.Handlers;
using EuroBank.Shared.Messaging;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using Wolverine;
using Wolverine.ErrorHandling;
using Wolverine.Kafka;
using Wolverine.Runtime;

namespace EuroBank.Antifraude.Infrastructure.Messaging;

public static class ConfiguracaoMensageriaWorker
{
    /// <summary>
    /// Wolverine no Antifraude.Worker. O perfil consome eurobank.contas.eventos sem inbox durável: a versão da conta no
    /// Redis já descarta o repetido, e reconstruir o perfil é só voltar o offset do consumer group. O processamento é
    /// em linha (o offset só avança depois de o Redis gravar). Com o Redis fora do ar, o listener pausa e tenta de novo.
    /// </summary>
    public static WolverineOptions ConfigurarMensageriaAntifraudeWorker(this WolverineOptions opcoes, IConfiguration configuracao)
    {
        var kafka = configuracao.GetSection(OpcoesKafka.Secao).Get<OpcoesKafka>() ?? new OpcoesKafka();
        if (string.IsNullOrWhiteSpace(kafka.GrupoConsumidor))
        {
            kafka.GrupoConsumidor = "eurobank-antifraude-worker";
        }

        var connectionString = configuracao.GetConnectionString("Antifraude")
            ?? throw new InvalidOperationException("ConnectionStrings:Antifraude não configurada.");

        opcoes.UsarConvencoesEuroBank(typeof(ContaAbertaManipulador).Assembly);

        // A persistência guarda o dead letter (e as próximas etapas usam a inbox durável em outros tópicos).
        opcoes.UsarInboxDuravel(connectionString);
        opcoes.UsarKafka(kafka);
        opcoes.ListenToKafkaTopic(TiposEventosContas.Topico)
            .UseInterop((IWolverineRuntime _, KafkaTopic _) => new MapeadorEnvelopeDebezium(TiposEventosContas.Mapa))
            .ProcessInline();

        // Dispositivo e localização das aprovadas: como o perfil de Contas, sem inbox (set e sorted set no Redis são
        // idempotentes pelo membro) e reconstruível voltando o offset.
        opcoes.ListenToKafkaTopic(TiposEventosAntifraude.Topico)
            .UseInterop((IWolverineRuntime _, KafkaTopic _) => new MapeadorEnvelopeDebezium(TiposEventosAntifraude.Mapa))
            .ProcessInline();

        // Cancelamento pelo cliente fecha o caso de revisão: grava no Postgres, então aqui a inbox durável vale.
        opcoes.OuvirTopicoDebezium(TiposEventosTransferencias.Topico, TiposEventosTransferencias.Mapa);

        opcoes.OnException<RedisConnectionException>().Or<RedisTimeoutException>()
            .RetryWithCooldown(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5))
            .Then.PauseThenRequeue(TimeSpan.FromSeconds(30));
        opcoes.AplicarPoliticaDeErros();

        return opcoes;
    }
}
