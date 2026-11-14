using EuroBank.Shared.TransferObjects;

namespace EuroBank.Notificacoes.Worker.Handlers;

/// <summary>
/// Tolerant reader: um evento novo em eurobank.transferencias.eventos não pode travar as notificações. Registra um
/// alerta e segue; o offset avança normalmente.
/// </summary>
public sealed class EventoDesconhecidoManipulador(ILogger<EventoDesconhecidoManipulador> logger)
{
    public void Handle(EventoDesconhecido evento) =>
        logger.LogWarning("Evento de tipo desconhecido {TipoEvento} ignorado", evento.Tipo);
}
