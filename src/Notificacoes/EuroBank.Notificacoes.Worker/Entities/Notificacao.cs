using EuroBank.Notificacoes.Worker.Enums;

namespace EuroBank.Notificacoes.Worker.Entities;

/// <summary>
/// Um e-mail por evento final de transferência. A chave é o id do evento (header "id" da outbox): reprocessar o
/// tópico, mesmo depois de a inbox do Wolverine esquecer a mensagem, encontra a notificação já Enviada e não
/// manda de novo.
/// </summary>
public sealed class Notificacao
{
    private const int TamanhoMaximoErro = 500;

    private Notificacao()
    {
        // Usado pelo EF Core.
    }

    /// <summary>Id do evento que gerou a notificação.</summary>
    public Guid Id { get; private set; }

    public Guid TransferenciaId { get; private set; }

    /// <summary>Nome do evento (ex.: TransferenciaConcluida).</summary>
    public string TipoEvento { get; private set; } = string.Empty;

    public string Destinatario { get; private set; } = string.Empty;

    public string Assunto { get; private set; } = string.Empty;

    public StatusNotificacao Status { get; private set; }

    /// <summary>Quantas vezes o SMTP foi chamado para este e-mail.</summary>
    public int Tentativas { get; private set; }

    public string? UltimoErro { get; private set; }

    public DateTimeOffset CriadaEm { get; private set; }

    public DateTimeOffset AtualizadaEm { get; private set; }

    public DateTimeOffset? EnviadaEm { get; private set; }

    public static Notificacao Criar(
        Guid eventoId,
        Guid transferenciaId,
        string tipoEvento,
        string destinatario,
        string assunto,
        DateTimeOffset agora) =>
        new()
        {
            Id = eventoId,
            TransferenciaId = transferenciaId,
            TipoEvento = tipoEvento,
            Destinatario = destinatario,
            Assunto = assunto,
            Status = StatusNotificacao.Pendente,
            CriadaEm = agora,
            AtualizadaEm = agora,
        };

    public void RegistrarEnvio(DateTimeOffset agora)
    {
        Tentativas++;
        Status = StatusNotificacao.Enviada;
        UltimoErro = null;
        EnviadaEm = agora;
        AtualizadaEm = agora;
    }

    public void RegistrarFalha(string erro, DateTimeOffset agora)
    {
        Tentativas++;
        UltimoErro = erro.Length > TamanhoMaximoErro ? erro[..TamanhoMaximoErro] : erro;
        AtualizadaEm = agora;
    }
}
