using EuroBank.Extrato.Application.TransferObjects;
using Microsoft.Extensions.Logging;

namespace EuroBank.Extrato.Application.Handlers;

/// <summary>
/// Tolerant reader: um tipo novo publicado por Contas não pode travar o consumo. Registra um alerta e segue;
/// o offset avança normalmente.
/// </summary>
public sealed class EventoDesconhecidoManipulador(ILogger<EventoDesconhecidoManipulador> logger)
{
    public void Handle(EventoDesconhecido evento) =>
        logger.LogWarning("Evento de tipo desconhecido {TipoEvento} ignorado", evento.Tipo);
}
