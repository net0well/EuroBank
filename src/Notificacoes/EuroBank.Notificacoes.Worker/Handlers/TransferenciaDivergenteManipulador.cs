using EuroBank.Notificacoes.Worker.Messaging;
using EuroBank.Notificacoes.Worker.Services;
using EuroBank.Notificacoes.Worker.Templates;
using EuroBank.Notificacoes.Worker.TransferObjects;
using Wolverine;

namespace EuroBank.Notificacoes.Worker.Handlers;

public sealed class TransferenciaDivergenteManipulador(ServicoNotificacoes notificacoes)
{
    public Task Handle(EventoTransferenciaDivergente evento, Envelope envelope, CancellationToken cancellationToken) =>
        notificacoes.NotificarAsync(
            envelope.Id,
            evento.TransferenciaId,
            TiposEventosTransferencias.Divergente,
            evento.Resumo?.EmailNotificacao,
            () => EmailTransferenciaDivergente.Montar(evento),
            cancellationToken);
}
