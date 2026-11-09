using EuroBank.Shared.TransferObjects;
using Microsoft.Extensions.Logging;

namespace EuroBank.Transferencias.Application.Handlers;

/// <summary>Tolerant reader: uma resposta de tipo novo não trava o consumo; registra um alerta e segue.</summary>
public sealed class EventoDesconhecidoManipulador(ILogger<EventoDesconhecidoManipulador> logger)
{
    public void Handle(EventoDesconhecido evento) =>
        logger.LogWarning("Mensagem de tipo desconhecido {TipoEvento} ignorada", evento.Tipo);
}
