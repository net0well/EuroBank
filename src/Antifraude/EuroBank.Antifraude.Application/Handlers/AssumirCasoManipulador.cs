using EuroBank.Antifraude.Application.Commands;
using EuroBank.Antifraude.Application.Extensions;
using EuroBank.Antifraude.Application.Interfaces;
using EuroBank.Antifraude.Application.TransferObjects;
using EuroBank.Antifraude.Domain.Repositories;
using EuroBank.Shared.Domain;

namespace EuroBank.Antifraude.Application.Handlers;

public sealed class AssumirCasoManipulador(
    IRepositorioCasos casos,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    IConsultaAntifraude consulta,
    TimeProvider relogio)
{
    public async Task<CasoDetalheDto> Handle(AssumirCaso comando, CancellationToken cancellationToken)
    {
        var caso = await casos.ObterOuFalharAsync(comando.CasoId, cancellationToken);
        caso.Assumir(comando.Analista?.Trim() ?? string.Empty, relogio.GetUtcNow());
        await unidadeDeTrabalho.SalvarAsync(cancellationToken);
        return (await consulta.ObterCasoAsync(comando.CasoId, cancellationToken))!;
    }
}
