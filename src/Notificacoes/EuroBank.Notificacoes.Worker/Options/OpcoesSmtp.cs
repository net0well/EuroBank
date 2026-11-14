namespace EuroBank.Notificacoes.Worker.Options;

/// <summary>Seção "Smtp": no laboratório, o Mailpit (SMTP na 1025, sem TLS e sem autenticação).</summary>
public sealed class OpcoesSmtp
{
    public const string Secao = "Smtp";

    public string Host { get; set; } = "localhost";

    public int Porta { get; set; } = 1025;

    public string Remetente { get; set; } = "nao-responda@eurobank.lab";

    public string NomeRemetente { get; set; } = "EuroBank";

    /// <summary>Tempo máximo de cada operação com o servidor (conectar, enviar).</summary>
    public int TimeoutSegundos { get; set; } = 10;
}
