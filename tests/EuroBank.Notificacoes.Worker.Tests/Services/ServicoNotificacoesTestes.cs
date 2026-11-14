using EuroBank.Notificacoes.Worker.Enums;
using EuroBank.Notificacoes.Worker.Exceptions;
using EuroBank.Notificacoes.Worker.Templates;
using EuroBank.Notificacoes.Worker.Tests.Fixtures;

namespace EuroBank.Notificacoes.Worker.Tests.Services;

/// <summary>Do evento ao e-mail: um e-mail por evento, nenhum sem endereço, e falha de SMTP registrada.</summary>
public sealed class ServicoNotificacoesTestes(NotificacoesWorkerFixture worker)
{
    [Fact]
    public async Task EventoComEmail_EnviaERegistraComoEnviada()
    {
        var email = Eventos.EmailUnico();
        var evento = Eventos.Concluida(Eventos.Resumo(email));

        await worker.EntregarAsync(evento);

        var enviado = worker.Enviador.EnviadosPara(email).ShouldHaveSingleItem();
        enviado.Conteudo.Assunto.ShouldStartWith("Transferência concluída");
        var notificacao = (await worker.NotificacoesDaTransferenciaAsync(evento.TransferenciaId)).ShouldHaveSingleItem();
        notificacao.Status.ShouldBe(StatusNotificacao.Enviada);
        notificacao.TipoEvento.ShouldBe("TransferenciaConcluida");
        notificacao.Destinatario.ShouldBe(email);
        notificacao.Assunto.ShouldBe(enviado.Conteudo.Assunto);
        notificacao.Tentativas.ShouldBe(1);
        notificacao.EnviadaEm.ShouldNotBeNull();
        notificacao.Id.ShouldBe(enviado.Id);
    }

    [Fact]
    public async Task CadaEventoFinal_TemOProprioEmail()
    {
        var email = Eventos.EmailUnico();
        var resumo = Eventos.Resumo(email);

        await worker.EntregarAsync(Eventos.Cancelada(resumo: resumo));
        await worker.EntregarAsync(Eventos.Recusada(resumo: resumo));
        await worker.EntregarAsync(Eventos.Divergente(resumo: resumo));
        await worker.EntregarAsync(Eventos.Devolvida(resumo));

        worker.Enviador.EnviadosPara(email).Select(enviado => enviado.Conteudo.Assunto.Split(':')[0]).ShouldBe(
            ["Transferência cancelada", "Transferência não realizada", "Transferência em análise", "Transferência devolvida"]);
    }

    [Fact]
    public async Task EventoSemEmail_NaoEnviaNemRegistra()
    {
        var evento = Eventos.Concluida(Eventos.Resumo(email: null));

        await worker.EntregarAsync(evento);

        (await worker.NotificacoesDaTransferenciaAsync(evento.TransferenciaId)).ShouldBeEmpty();
    }

    [Fact]
    public async Task EventoSemResumo_PublicadoAntesDasNotificacoes_EhIgnorado()
    {
        var evento = Eventos.Recusada() with { Resumo = null };

        await worker.EntregarAsync(evento);

        (await worker.NotificacoesDaTransferenciaAsync(evento.TransferenciaId)).ShouldBeEmpty();
    }

    [Fact]
    public async Task EnderecoInvalido_NaoEnvia()
    {
        var evento = Eventos.Recusada(resumo: Eventos.Resumo(email: "isto não é um e-mail"));

        await worker.EntregarAsync(evento);

        (await worker.NotificacoesDaTransferenciaAsync(evento.TransferenciaId)).ShouldBeEmpty();
    }

    [Fact]
    public async Task MesmoEventoDuasVezes_EnviaUmaSo()
    {
        var email = Eventos.EmailUnico();
        var evento = Eventos.Concluida(Eventos.Resumo(email));
        var eventoId = Guid.CreateVersion7();

        for (var vez = 0; vez < 2; vez++)
        {
            await worker.ComServicoAsync(servico => servico.NotificarAsync(
                eventoId,
                evento.TransferenciaId,
                "TransferenciaConcluida",
                email,
                () => EmailTransferenciaConcluida.Montar(evento),
                TestContext.Current.CancellationToken));
        }

        worker.Enviador.EnviadosPara(email).ShouldHaveSingleItem().Id.ShouldBe(eventoId);
        (await worker.NotificacoesDaTransferenciaAsync(evento.TransferenciaId)).ShouldHaveSingleItem().Tentativas.ShouldBe(1);
    }

    [Fact]
    public async Task FalhaDeSmtp_FicaPendenteComOErro_ESobeParaAPoliticaDeErros()
    {
        var email = Eventos.EmailUnico();
        var evento = Eventos.Recusada(resumo: Eventos.Resumo(email));
        worker.Enviador.FalharProximas(email, 1);

        // Direto no serviço: pelo barramento, a política de erros já tentaria de novo (ver RetryEnvioTestes).
        await Should.ThrowAsync<ExcecaoEnvioEmail>(() => worker.ComServicoAsync(servico => servico.NotificarAsync(
            Guid.CreateVersion7(),
            evento.TransferenciaId,
            "TransferenciaRecusada",
            email,
            () => EmailTransferenciaRecusada.Montar(evento),
            TestContext.Current.CancellationToken)));

        var pendente = (await worker.NotificacoesDaTransferenciaAsync(evento.TransferenciaId)).ShouldHaveSingleItem();
        pendente.Status.ShouldBe(StatusNotificacao.Pendente);
        pendente.Tentativas.ShouldBe(1);
        pendente.EnviadaEm.ShouldBeNull();
        pendente.UltimoErro.ShouldNotBeNull().ShouldContain("SMTP indisponível");
        worker.Enviador.EnviadosPara(email).ShouldBeEmpty();
    }
}
