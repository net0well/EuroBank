using EuroBank.Notificacoes.Worker.Messaging;
using EuroBank.Notificacoes.Worker.Services;
using EuroBank.Notificacoes.Worker.Templates;
using EuroBank.Notificacoes.Worker.TransferObjects;
using Wolverine;

namespace EuroBank.Notificacoes.Worker.Handlers;

public sealed class TransferenciaDevolvidaManipulador(ServicoNotificacoes notificacoes)
{
    public Task Handle(EventoTransferenciaDevolvida evento, Envelope envelope, CancellationToken cancellationToken) =>
        notificacoes.NotificarAsync(
            envelope.Id,
            evento.TransferenciaId,
            TiposEventosTransferencias.Devolvida,
            evento.Resumo?.EmailNotificacao,
            () => EmailTransferenciaDevolvida.Montar(evento),
            cancellationToken);
}
