using EuroBank.Antifraude.Application.Commands;
using EuroBank.Antifraude.Application.Extensions;
using EuroBank.Antifraude.Domain.Repositories;
using EuroBank.Shared.Domain;
using Microsoft.Extensions.Logging;

namespace EuroBank.Antifraude.Application.Handlers;

/// <summary>
/// Expira o caso e a análise juntos (AnaliseDecidida Expirada vai para a saga, que termina RevisaoExpirada). Se o
/// analista decidiu no mesmo instante, a versão do caso faz um dos dois perder; o caso já decidido não expira.
/// </summary>
public sealed class ExpirarCasoManipulador(
    IRepositorioCasos casos,
    IRepositorioAnalises analises,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    TimeProvider relogio,
    ILogger<ExpirarCasoManipulador> logger)
{
    public async Task Handle(ExpirarCaso comando, CancellationToken cancellationToken)
    {
        var agora = relogio.GetUtcNow();
        var caso = await casos.ObterOuFalharAsync(comando.CasoId, cancellationToken);
        if (!caso.Expirar(agora))
        {
            return;
        }

        (await analises.ObterDoCasoAsync(caso, cancellationToken)).Expirar(agora);
        await unidadeDeTrabalho.SalvarAsync(cancellationToken);
        logger.LogInformation("Caso {CasoId} da operação {OperacaoId} expirou sem decisão", caso.Id, caso.OperacaoId);
    }
}
