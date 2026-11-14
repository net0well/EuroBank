using EuroBank.Notificacoes.Worker.Enums;
using EuroBank.Notificacoes.Worker.Tests.Fixtures;

namespace EuroBank.Notificacoes.Worker.Tests.Services;

/// <summary>
/// Política de erros do worker: SMTP fora do ar ganha novas tentativas agendadas (no teste, 100, 200 e 300 ms) e,
/// esgotadas, a mensagem vai para o dead letter com a notificação ainda Pendente.
/// </summary>
public sealed class RetryEnvioTestes(NotificacoesWorkerFixture worker)
{
    private static readonly TimeSpan Limite = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task SmtpVoltaNaTerceiraTentativa_EmailSaiUmaVez()
    {
        var email = Eventos.EmailUnico();
        var evento = Eventos.Recusada(resumo: Eventos.Resumo(email));
        worker.Enviador.FalharProximas(email, 2);

        await worker.PublicarAsync(evento);

        await Esperar.AteAsync(
            async () => (await worker.NotificacoesDaTransferenciaAsync(evento.TransferenciaId))
                .SingleOrDefault()?.Status == StatusNotificacao.Enviada,
            Limite,
            "a notificação não chegou a Enviada");
        (await worker.NotificacoesDaTransferenciaAsync(evento.TransferenciaId)).Single().Tentativas.ShouldBe(3);
        worker.Enviador.EnviadosPara(email).ShouldHaveSingleItem();
    }

    [Fact]
    public async Task SmtpForaDoArSempre_DesisteDepoisDeQuatroTentativas()
    {
        var email = Eventos.EmailUnico();
        var evento = Eventos.Cancelada(resumo: Eventos.Resumo(email));
        worker.Enviador.FalharProximas(email, 100);

        await worker.PublicarAsync(evento);

        await Esperar.AteAsync(
            async () => (await worker.NotificacoesDaTransferenciaAsync(evento.TransferenciaId))
                .SingleOrDefault()?.Tentativas == 4,
            Limite,
            "a notificação não chegou a 4 tentativas");

        // Esgotadas as esperas configuradas, não há quinta tentativa.
        await Task.Delay(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
        var pendente = (await worker.NotificacoesDaTransferenciaAsync(evento.TransferenciaId)).Single();
        pendente.Tentativas.ShouldBe(4);
        pendente.Status.ShouldBe(StatusNotificacao.Pendente);
        worker.Enviador.EnviadosPara(email).ShouldBeEmpty();
    }
}
