namespace EuroBank.Notificacoes.Worker.Exceptions;

/// <summary>
/// O SMTP não aceitou o e-mail (fora do ar, recusou, demorou). É transitória: a política de erros agenda novas
/// tentativas e, esgotadas, manda a mensagem para o dead letter.
/// </summary>
public sealed class ExcecaoEnvioEmail : Exception
{
    public ExcecaoEnvioEmail(string mensagem)
        : base(mensagem)
    {
    }

    public ExcecaoEnvioEmail(string mensagem, Exception causa)
        : base(mensagem, causa)
    {
    }
}
