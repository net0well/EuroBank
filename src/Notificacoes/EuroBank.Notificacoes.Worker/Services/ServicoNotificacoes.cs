using System.Net.Mail;
using EuroBank.Notificacoes.Worker.Entities;
using EuroBank.Notificacoes.Worker.Enums;
using EuroBank.Notificacoes.Worker.Exceptions;
using EuroBank.Notificacoes.Worker.Persistence;
using EuroBank.Notificacoes.Worker.TransferObjects;

namespace EuroBank.Notificacoes.Worker.Services;

/// <summary>
/// Um e-mail por evento final. A notificação é gravada Pendente antes de chamar o SMTP e só vira Enviada depois de
/// ele aceitar: um evento repetido (reprocessamento do tópico, reenvio) encontra a Enviada e não manda de novo.
/// </summary>
/// <remarks>
/// O SMTP não participa da transação do banco: se o processo cair entre o SMTP aceitar e a notificação virar Enviada,
/// a próxima tentativa manda outra cópia (com o mesmo Message-ID). É o limite do "pelo menos uma vez".
/// </remarks>
public sealed class ServicoNotificacoes(
    NotificacoesDbContext contexto,
    IEnviadorEmail enviador,
    TimeProvider relogio,
    ILogger<ServicoNotificacoes> logger)
{
    public async Task NotificarAsync(
        Guid eventoId,
        Guid transferenciaId,
        string tipoEvento,
        string? destinatario,
        Func<ConteudoEmail> montar,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(destinatario))
        {
            logger.LogDebug("{TipoEvento} da transferência {TransferenciaId} sem e-mail de notificação", tipoEvento, transferenciaId);
            return;
        }

        if (!MailAddress.TryCreate(destinatario, out _))
        {
            logger.LogWarning(
                "{TipoEvento} da transferência {TransferenciaId} com e-mail de notificação inválido: nada enviado",
                tipoEvento,
                transferenciaId);
            return;
        }

        var notificacao = await contexto.Notificacoes.FindAsync([eventoId], cancellationToken);
        if (notificacao?.Status == StatusNotificacao.Enviada)
        {
            logger.LogInformation(
                "{TipoEvento} da transferência {TransferenciaId} já notificado (evento {EventoId}): ignorado",
                tipoEvento,
                transferenciaId,
                eventoId);
            return;
        }

        var conteudo = montar();
        if (notificacao is null)
        {
            notificacao = Notificacao.Criar(eventoId, transferenciaId, tipoEvento, destinatario, conteudo.Assunto, relogio.GetUtcNow());
            contexto.Notificacoes.Add(notificacao);
            await contexto.SaveChangesAsync(cancellationToken);
        }

        try
        {
            await enviador.EnviarAsync(new EmailParaEnviar(eventoId, destinatario, conteudo), cancellationToken);
        }
        catch (ExcecaoEnvioEmail erro)
        {
            notificacao.RegistrarFalha(erro.Message, relogio.GetUtcNow());
            await contexto.SaveChangesAsync(CancellationToken.None);
            logger.LogWarning(
                "E-mail {TipoEvento} da transferência {TransferenciaId} não enviado (tentativa {Tentativa}): {Erro}",
                tipoEvento,
                transferenciaId,
                notificacao.Tentativas,
                erro.Message);
            throw;
        }

        notificacao.RegistrarEnvio(relogio.GetUtcNow());
        await contexto.SaveChangesAsync(CancellationToken.None);
        logger.LogInformation(
            "E-mail {TipoEvento} da transferência {TransferenciaId} enviado (tentativa {Tentativa})",
            tipoEvento,
            transferenciaId,
            notificacao.Tentativas);
    }
}
