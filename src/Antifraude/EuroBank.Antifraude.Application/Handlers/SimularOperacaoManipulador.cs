using EuroBank.Antifraude.Application.Commands;
using EuroBank.Antifraude.Application.Mappers;
using EuroBank.Antifraude.Application.Services;
using EuroBank.Antifraude.Application.TransferObjects;

namespace EuroBank.Antifraude.Application.Handlers;

public sealed class SimularOperacaoManipulador(AvaliadorOperacoes avaliador, TimeProvider relogio)
{
    public async Task<ResultadoAvaliacaoDto> Handle(SimularOperacao comando, CancellationToken cancellationToken)
    {
        var resultado = await avaliador.DecidirAsync(comando.Contexto, cancellationToken);
        return resultado.ParaSimulacao(comando.Contexto, relogio.GetUtcNow());
    }
}
