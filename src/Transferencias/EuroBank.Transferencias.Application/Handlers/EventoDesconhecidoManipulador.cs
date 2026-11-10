using EuroBank.Shared.TransferObjects;
using Microsoft.Extensions.Logging;

namespace EuroBank.Transferencias.Application.Handlers;

/// <summary>
/// Tolerant reader: um tipo que não interessa não trava o consumo. Em eurobank.contas.eventos a maioria dos eventos
/// (depósitos, saques...) não interessa a Transferencias, então o registro é só em Debug.
/// </summary>
public sealed class EventoDesconhecidoManipulador(ILogger<EventoDesconhecidoManipulador> logger)
{
    public void Handle(EventoDesconhecido evento) =>
        logger.LogDebug("Mensagem de tipo {TipoEvento} ignorada", evento.Tipo);
}
