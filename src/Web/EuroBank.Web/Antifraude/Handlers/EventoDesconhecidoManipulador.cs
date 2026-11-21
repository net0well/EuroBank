using EuroBank.Shared.TransferObjects;

namespace EuroBank.Web.Antifraude.Handlers;

/// <summary>Tolerant reader: um tipo novo de evento no tópico não trava o consumo do painel.</summary>
public sealed class EventoDesconhecidoManipulador(ILogger<EventoDesconhecidoManipulador> logger)
{
    public void Handle(EventoDesconhecido evento) =>
        logger.LogDebug("Evento de tipo {TipoEvento} ignorado pelo painel", evento.Tipo);
}
