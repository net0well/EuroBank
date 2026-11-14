using EuroBank.Antifraude.Application.Extensions;
using EuroBank.Antifraude.Application.TransferObjects;
using EuroBank.Antifraude.Domain.Repositories;
using EuroBank.Shared.Domain;
using Microsoft.Extensions.Logging;

namespace EuroBank.Antifraude.Application.Handlers;

/// <summary>
/// O cliente cancelou a transferência durante a revisão: o caso pendente sai da fila e a análise fecha como Cancelada.
/// Outros motivos de cancelamento (estorno pelo banco, por exemplo) não mexem no antifraude.
/// </summary>
public sealed class TransferenciaCanceladaManipulador(
    IRepositorioCasos casos,
    IRepositorioAnalises analises,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    TimeProvider relogio,
    ILogger<TransferenciaCanceladaManipulador> logger)
{
    public const string CanceladaPeloCliente = "CanceladaPeloCliente";

    public async Task Handle(EventoTransferenciaCancelada evento, CancellationToken cancellationToken)
    {
        if (evento.Motivo != CanceladaPeloCliente)
        {
            return;
        }

        var caso = await casos.ObterPorOperacaoAsync(evento.TransferenciaId, cancellationToken);
        var agora = relogio.GetUtcNow();
        if (caso is null || !caso.CancelarPeloCliente(agora))
        {
            return;
        }

        (await analises.ObterDoCasoAsync(caso, cancellationToken)).Cancelar(agora);
        await unidadeDeTrabalho.SalvarAsync(cancellationToken);
        logger.LogInformation("Caso {CasoId} fechado: a transferência {TransferenciaId} foi cancelada pelo cliente", caso.Id, evento.TransferenciaId);
    }
}
