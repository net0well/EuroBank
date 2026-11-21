using EuroBank.Antifraude.Application.Interfaces;
using EuroBank.Antifraude.Application.TransferObjects;

namespace EuroBank.Antifraude.Application.Handlers;

/// <summary>
/// Uma operação aprovada (pelo motor ou pelo analista) ensina ao perfil o dispositivo e a localização dela: são as
/// referências de dispositivo novo, país habitual e viagem impossível. Recusadas, expiradas e canceladas não ensinam.
/// </summary>
public sealed class AnaliseDecididaManipulador(IPerfisContas perfis)
{
    public async Task Handle(EventoAnaliseDecidida evento, CancellationToken cancellationToken)
    {
        if (evento.Decisao != "Aprovada" || (evento.DispositivoId is null && evento.Localizacao is null))
        {
            return;
        }

        await perfis.RegistrarUsoAsync(
            new UsoConta(evento.ContaId, evento.AnaliseId, evento.MomentoOperacao, evento.DispositivoId, evento.Localizacao),
            cancellationToken);
    }
}
