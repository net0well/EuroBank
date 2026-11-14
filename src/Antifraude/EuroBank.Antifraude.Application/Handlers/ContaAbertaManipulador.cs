using EuroBank.Antifraude.Application.Extensions;
using EuroBank.Antifraude.Application.Interfaces;
using EuroBank.Antifraude.Application.TransferObjects;
using Microsoft.Extensions.Logging;

namespace EuroBank.Antifraude.Application.Handlers;

public sealed class ContaAbertaManipulador(IPerfisContas perfis, ILogger<ContaAbertaManipulador> logger)
{
    public async Task Handle(EventoContaAberta evento, CancellationToken cancellationToken)
    {
        var aplicado = await perfis.AplicarAsync(
            new AtualizacaoPerfil(evento.ContaId, evento.Versao, evento.OcorridoEm, SaldoApos: 0m, AbertaEm: evento.OcorridoEm, Moeda: evento.Moeda ?? "EUR"),
            cancellationToken);
        logger.RegistrarPerfil(aplicado, "ContaAberta", evento.ContaId, evento.Versao);
    }
}
