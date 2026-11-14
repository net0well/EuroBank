using EuroBank.Notificacoes.Worker.Exceptions;
using EuroBank.Notificacoes.Worker.Options;
using EuroBank.Shared.Messaging;
using Wolverine;
using Wolverine.ErrorHandling;

namespace EuroBank.Notificacoes.Worker.Messaging;

public static class ConfiguracaoMensageria
{
    /// <summary>
    /// Wolverine nas notificações: consome eurobank.transferencias.eventos com inbox durável no Postgres. SMTP fora do
    /// ar ganha novas tentativas agendadas (a mensagem sai da partição e volta depois, sem segurar as outras); as
    /// demais falhas seguem a política comum do laboratório.
    /// </summary>
    public static WolverineOptions ConfigurarMensageriaNotificacoes(this WolverineOptions opcoes, IConfiguration configuracao)
    {
        var kafka = configuracao.GetSection(OpcoesKafka.Secao).Get<OpcoesKafka>() ?? new OpcoesKafka();
        if (string.IsNullOrWhiteSpace(kafka.GrupoConsumidor))
        {
            kafka.GrupoConsumidor = "eurobank-notificacoes";
        }

        var connectionString = configuracao.GetConnectionString("Notificacoes")
            ?? throw new InvalidOperationException("ConnectionStrings:Notificacoes não configurada.");
        var notificacoes = configuracao.GetSection(OpcoesNotificacoes.Secao).Get<OpcoesNotificacoes>() ?? new OpcoesNotificacoes();

        opcoes.UsarConvencoesEuroBank(typeof(MarcadorNotificacoesWorker).Assembly);
        opcoes.UsarInboxDuravel(connectionString);
        opcoes.UsarKafka(kafka);
        opcoes.OuvirTopicoDebezium(TiposEventosTransferencias.Topico, TiposEventosTransferencias.Mapa);

        // A regra mais específica vem antes da política comum.
        opcoes.OnException<ExcecaoEnvioEmail>()
            .ScheduleRetry(notificacoes.EsperasEfetivas)
            .Then.MoveToErrorQueue();
        opcoes.AplicarPoliticaDeErros();

        return opcoes;
    }
}
