using System.Net.Sockets;
using EuroBank.Notificacoes.Worker.Exceptions;
using EuroBank.Notificacoes.Worker.Options;
using EuroBank.Notificacoes.Worker.TransferObjects;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace EuroBank.Notificacoes.Worker.Services;

/// <summary>
/// Envio pelo MailKit, uma conexão por e-mail (o volume do laboratório é pequeno). O Message-ID leva o id do
/// evento: se o mesmo e-mail sair duas vezes (falha depois de o SMTP aceitar), as duas cópias têm o mesmo Message-ID.
/// </summary>
public sealed class EnviadorEmailSmtp(IOptions<OpcoesSmtp> opcoes) : IEnviadorEmail
{
    public async Task EnviarAsync(EmailParaEnviar email, CancellationToken cancellationToken)
    {
        var smtp = opcoes.Value;
        using var mensagem = new MimeMessage();
        mensagem.From.Add(new MailboxAddress(smtp.NomeRemetente, smtp.Remetente));
        mensagem.To.Add(MailboxAddress.Parse(email.Destinatario));
        mensagem.Subject = email.Conteudo.Assunto;
        mensagem.MessageId = $"{email.Id:N}@{DominioDoRemetente(smtp.Remetente)}";
        mensagem.Body = new BodyBuilder { HtmlBody = email.Conteudo.Html, TextBody = email.Conteudo.Texto }.ToMessageBody();

        using var cliente = new SmtpClient { Timeout = (int)TimeSpan.FromSeconds(smtp.TimeoutSegundos).TotalMilliseconds };
        try
        {
            await cliente.ConnectAsync(smtp.Host, smtp.Porta, SecureSocketOptions.None, cancellationToken);
            await cliente.SendAsync(mensagem, cancellationToken);
            await cliente.DisconnectAsync(quit: true, cancellationToken);
        }
        catch (Exception erro) when (erro is SocketException or IOException or TimeoutException
            or CommandException or ProtocolException or ServiceNotConnectedException)
        {
            throw new ExcecaoEnvioEmail($"o SMTP {smtp.Host}:{smtp.Porta} não aceitou o e-mail: {erro.Message}", erro);
        }
    }

    private static string DominioDoRemetente(string remetente)
    {
        var arroba = remetente.LastIndexOf('@');
        return arroba >= 0 ? remetente[(arroba + 1)..] : "eurobank.lab";
    }
}
