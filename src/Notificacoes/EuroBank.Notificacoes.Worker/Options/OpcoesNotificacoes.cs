namespace EuroBank.Notificacoes.Worker.Options;

/// <summary>Seção "Notificacoes".</summary>
public sealed class OpcoesNotificacoes
{
    public const string Secao = "Notificacoes";

    /// <summary>Usadas quando a configuração não informa nenhuma espera.</summary>
    public static readonly TimeSpan[] EsperasPadrao =
        [TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(2)];

    /// <summary>
    /// Esperas antes de cada nova tentativa quando o SMTP falha. Esgotadas, a mensagem vai para o dead letter.
    /// Sem padrão na propriedade: o binder de configuração acrescentaria os valores do appsettings aos padrões.
    /// </summary>
    public TimeSpan[] EsperasEntreTentativas { get; set; } = [];

    public TimeSpan[] EsperasEfetivas => EsperasEntreTentativas.Length > 0 ? EsperasEntreTentativas : EsperasPadrao;
}
