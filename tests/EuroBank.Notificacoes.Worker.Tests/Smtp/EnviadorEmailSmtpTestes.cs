using EuroBank.Notificacoes.Worker.Exceptions;
using EuroBank.Notificacoes.Worker.Options;
using EuroBank.Notificacoes.Worker.Services;
using EuroBank.Notificacoes.Worker.Templates;
using EuroBank.Notificacoes.Worker.Tests.Fixtures;
using EuroBank.Notificacoes.Worker.TransferObjects;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace EuroBank.Notificacoes.Worker.Tests.Smtp;

/// <summary>Envio pelo MailKit para um Mailpit de verdade.</summary>
public sealed class EnviadorEmailSmtpTestes(MailpitFixture mailpit) : IClassFixture<MailpitFixture>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Envia_HtmlETexto_ComOIdDoEventoNoMessageId()
    {
        var destinatario = Eventos.EmailUnico();
        var eventoId = Guid.CreateVersion7();
        var conteudo = EmailTransferenciaConcluida.Montar(Eventos.Concluida());
        var enviador = new EnviadorEmailSmtp(MsOptions.Create(new OpcoesSmtp { Host = mailpit.Host, Porta = mailpit.PortaSmtp }));

        await enviador.EnviarAsync(new EmailParaEnviar(eventoId, destinatario, conteudo), Ct);

        var resumo = (await mailpit.MensagensParaAsync(destinatario)).ShouldHaveSingleItem();
        var mensagem = await mailpit.MensagemAsync(resumo.GetProperty("ID").GetString()!);
        mensagem.GetProperty("Subject").GetString().ShouldBe(conteudo.Assunto);
        mensagem.GetProperty("MessageID").GetString().ShouldBe($"{eventoId:N}@eurobank.lab");
        mensagem.GetProperty("From").GetProperty("Address").GetString().ShouldBe("nao-responda@eurobank.lab");
        mensagem.GetProperty("From").GetProperty("Name").GetString().ShouldBe("EuroBank");
        mensagem.GetProperty("HTML").GetString()!.ShouldContain("1.234,50 EUR");
        mensagem.GetProperty("Text").GetString()!.ShouldContain("Valor enviado: 1.234,50 EUR");
    }

    [Fact]
    public async Task ServidorForaDoAr_LancaExcecaoDeEnvio()
    {
        // Porta sem ninguém ouvindo: a conexão é recusada na hora.
        var enviador = new EnviadorEmailSmtp(MsOptions.Create(new OpcoesSmtp { Host = "127.0.0.1", Porta = 1, TimeoutSegundos = 5 }));
        var email = new EmailParaEnviar(Guid.CreateVersion7(), Eventos.EmailUnico(), EmailTransferenciaRecusada.Montar(Eventos.Recusada()));

        await Should.ThrowAsync<ExcecaoEnvioEmail>(() => enviador.EnviarAsync(email, Ct));
    }
}
