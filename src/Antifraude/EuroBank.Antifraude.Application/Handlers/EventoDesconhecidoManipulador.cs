using EuroBank.Shared.TransferObjects;
using Microsoft.Extensions.Logging;

namespace EuroBank.Antifraude.Application.Handlers;

/// <summary>Tolerant reader: um tipo novo de evento não pode travar o consumo. Registra um alerta e segue.</summary>
public sealed class EventoDesconhecidoManipulador(ILogger<EventoDesconhecidoManipulador> logger)
{
    public void Handle(EventoDesconhecido evento) =>
        logger.LogWarning("Evento de tipo desconhecido {TipoEvento} ignorado", evento.Tipo);
}
