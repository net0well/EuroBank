using EuroBank.Antifraude.Application.Extensions;
using EuroBank.Antifraude.Application.Interfaces;
using EuroBank.Antifraude.Application.TransferObjects;
using Microsoft.Extensions.Logging;

namespace EuroBank.Antifraude.Application.Handlers;

public sealed class MovimentoContaManipulador(IPerfisContas perfis, ILogger<MovimentoContaManipulador> logger)
{
    public async Task Handle(EventoMovimentoConta evento, CancellationToken cancellationToken)
    {
        var aplicado = await perfis.AplicarAsync(
            new AtualizacaoPerfil(evento.ContaId, evento.Versao, evento.OcorridoEm, SaldoApos: evento.SaldoApos),
            cancellationToken);
        logger.RegistrarPerfil(aplicado, "Movimento", evento.ContaId, evento.Versao);
    }
}
