using EuroBank.Antifraude.Application.Extensions;
using EuroBank.Antifraude.Application.Interfaces;
using EuroBank.Antifraude.Application.TransferObjects;
using Microsoft.Extensions.Logging;

namespace EuroBank.Antifraude.Application.Handlers;

public sealed class StatusContaManipulador(IPerfisContas perfis, ILogger<StatusContaManipulador> logger)
{
    public async Task Handle(EventoStatusConta evento, CancellationToken cancellationToken)
    {
        var aplicado = await perfis.AplicarAsync(new AtualizacaoPerfil(evento.ContaId, evento.Versao, evento.OcorridoEm), cancellationToken);
        logger.RegistrarPerfil(aplicado, "Status", evento.ContaId, evento.Versao);
    }
}
