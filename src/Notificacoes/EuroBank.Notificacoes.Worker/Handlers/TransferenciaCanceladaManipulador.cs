using EuroBank.Notificacoes.Worker.Messaging;
using EuroBank.Notificacoes.Worker.Services;
using EuroBank.Notificacoes.Worker.Templates;
using EuroBank.Notificacoes.Worker.TransferObjects;
using Wolverine;

namespace EuroBank.Notificacoes.Worker.Handlers;

public sealed class TransferenciaCanceladaManipulador(ServicoNotificacoes notificacoes)
{
    public Task Handle(EventoTransferenciaCancelada evento, Envelope envelope, CancellationToken cancellationToken) =>
        notificacoes.NotificarAsync(
            envelope.Id,
            evento.TransferenciaId,
            TiposEventosTransferencias.Cancelada,
            evento.Resumo?.EmailNotificacao,
            () => EmailTransferenciaCancelada.Montar(evento),
            cancellationToken);
}
