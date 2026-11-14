using System.Collections.Concurrent;
using EuroBank.Notificacoes.Worker.Exceptions;
using EuroBank.Notificacoes.Worker.Services;
using EuroBank.Notificacoes.Worker.TransferObjects;

namespace EuroBank.Notificacoes.Worker.Tests.Fixtures;

/// <summary>Guarda os e-mails em memória e simula o SMTP fora do ar para um destinatário.</summary>
public sealed class EnviadorEmailFalso : IEnviadorEmail
{
    private readonly ConcurrentQueue<EmailParaEnviar> _enviados = new();
    private readonly ConcurrentDictionary<string, int> _falhasRestantes = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<EmailParaEnviar> EnviadosPara(string destinatario) =>
        _enviados.Where(email => string.Equals(email.Destinatario, destinatario, StringComparison.OrdinalIgnoreCase)).ToList();

    /// <summary>As próximas <paramref name="vezes"/> tentativas para o destinatário falham como um SMTP fora do ar.</summary>
    public void FalharProximas(string destinatario, int vezes) => _falhasRestantes[destinatario] = vezes;

    public Task EnviarAsync(EmailParaEnviar email, CancellationToken cancellationToken)
    {
        if (_falhasRestantes.TryGetValue(email.Destinatario, out var falhas) && falhas > 0)
        {
            _falhasRestantes[email.Destinatario] = falhas - 1;
            throw new ExcecaoEnvioEmail("servidor SMTP indisponível (simulado)");
        }

        _enviados.Enqueue(email);
        return Task.CompletedTask;
    }
}
