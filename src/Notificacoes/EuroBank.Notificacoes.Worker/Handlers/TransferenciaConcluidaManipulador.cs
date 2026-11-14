using EuroBank.Notificacoes.Worker.Messaging;
using EuroBank.Notificacoes.Worker.Services;
using EuroBank.Notificacoes.Worker.Templates;
using EuroBank.Notificacoes.Worker.TransferObjects;
using Wolverine;

namespace EuroBank.Notificacoes.Worker.Handlers;

/// <summary>O id do envelope é o id do evento na outbox (header "id"): é a chave da notificação.</summary>
public sealed class TransferenciaConcluidaManipulador(ServicoNotificacoes notificacoes)
{
    public Task Handle(EventoTransferenciaConcluida evento, Envelope envelope, CancellationToken cancellationToken) =>
        notificacoes.NotificarAsync(
            envelope.Id,
            evento.TransferenciaId,
            TiposEventosTransferencias.Concluida,
            evento.Resumo?.EmailNotificacao,
            () => EmailTransferenciaConcluida.Montar(evento),
            cancellationToken);
}
