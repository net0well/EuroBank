namespace EuroBank.Notificacoes.Worker.Enums;

public enum StatusNotificacao
{
    /// <summary>Registrada, mas o SMTP ainda não aceitou o e-mail (falhou ou está tentando).</summary>
    Pendente = 1,

    /// <summary>O SMTP aceitou o e-mail; o mesmo evento não gera outro.</summary>
    Enviada = 2,
}
