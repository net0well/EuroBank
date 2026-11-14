using EuroBank.Notificacoes.Worker.Messaging;
using EuroBank.Notificacoes.Worker.Services;
using EuroBank.Notificacoes.Worker.Templates;
using EuroBank.Notificacoes.Worker.TransferObjects;
using Wolverine;

namespace EuroBank.Notificacoes.Worker.Handlers;

public sealed class TransferenciaRecusadaManipulador(ServicoNotificacoes notificacoes)
{
    public Task Handle(EventoTransferenciaRecusada evento, Envelope envelope, CancellationToken cancellationToken) =>
        notificacoes.NotificarAsync(
            envelope.Id,
            evento.TransferenciaId,
            TiposEventosTransferencias.Recusada,
            evento.Resumo?.EmailNotificacao,
            () => EmailTransferenciaRecusada.Montar(evento),
            cancellationToken);
}
