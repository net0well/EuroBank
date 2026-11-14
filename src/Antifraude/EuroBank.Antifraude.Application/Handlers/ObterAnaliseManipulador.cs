using EuroBank.Antifraude.Application.Mappers;
using EuroBank.Antifraude.Application.Queries;
using EuroBank.Antifraude.Application.TransferObjects;
using EuroBank.Antifraude.Domain.Exceptions;
using EuroBank.Antifraude.Domain.Repositories;
using EuroBank.Antifraude.Domain.ValueObjects;

namespace EuroBank.Antifraude.Application.Handlers;

public sealed class ObterAnaliseManipulador(IRepositorioAnalises analises, IRepositorioCasos casos)
{
    public async Task<ResultadoAvaliacaoDto> Handle(ObterAnalise consulta, CancellationToken cancellationToken)
    {
        var analise = await analises.ObterAsync(new AnaliseId(consulta.AnaliseId), cancellationToken)
            ?? throw new ExcecaoAnaliseNaoEncontrada(consulta.AnaliseId);
        return analise.ParaDto(await casos.ObterPorOperacaoAsync(analise.OperacaoId, cancellationToken));
    }
}
