using EuroBank.Notificacoes.Worker.TransferObjects;

namespace EuroBank.Notificacoes.Worker.Services;

public interface IEnviadorEmail
{
    /// <summary>Entrega o e-mail ao SMTP; falha de envio vira <see cref="Exceptions.ExcecaoEnvioEmail"/>.</summary>
    Task EnviarAsync(EmailParaEnviar email, CancellationToken cancellationToken);
}
