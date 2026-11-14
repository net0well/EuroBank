using EuroBank.Antifraude.Application.Commands;
using EuroBank.Antifraude.Application.Mappers;
using EuroBank.Antifraude.Application.Options;
using EuroBank.Antifraude.Application.Services;
using EuroBank.Antifraude.Application.TransferObjects;
using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Repositories;
using EuroBank.Shared.Domain;
using EuroBank.Shared.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EuroBank.Antifraude.Application.Handlers;

/// <summary>
/// Avalia e grava a análise (e o caso, se for para revisão), com os eventos na outbox. A mesma operação avaliada de
/// novo, inclusive ao mesmo tempo, devolve a primeira análise: o índice único da operação decide quem grava.
/// </summary>
public sealed class AvaliarOperacaoManipulador(
    IRepositorioAnalises analises,
    IRepositorioCasos casos,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    AvaliadorOperacoes avaliador,
    IOptions<OpcoesRevisao> revisao,
    TimeProvider relogio,
    ILogger<AvaliarOperacaoManipulador> logger)
{
    public async Task<ResultadoAvaliacaoDto> Handle(AvaliarOperacao comando, CancellationToken cancellationToken)
    {
        var operacaoId = comando.Contexto.OperacaoId;
        if (await ExistenteAsync(operacaoId, cancellationToken) is { } existente)
        {
            return existente;
        }

        var agora = relogio.GetUtcNow();
        var resultado = await avaliador.DecidirAsync(comando.Contexto, cancellationToken);
        var analise = Analise.Registrar(comando.Contexto, resultado, agora);
        analises.Adicionar(analise);

        CasoRevisao? caso = null;
        if (analise.EstaEmRevisao)
        {
            caso = CasoRevisao.Abrir(analise.Id, operacaoId, analise.ContaId, analise.ScoreFinal, revisao.Value.PrazoCaso, agora);
            casos.Adicionar(caso);
        }

        try
        {
            await unidadeDeTrabalho.SalvarAsync(cancellationToken);
        }
        catch (ExcecaoConcorrencia)
        {
            // Outra avaliação da mesma operação gravou primeiro: devolve a dela.
            logger.LogInformation("Operação {OperacaoId} avaliada ao mesmo tempo por outra chamada; devolvendo a análise gravada", operacaoId);
            return await ExistenteAsync(operacaoId, cancellationToken)
                ?? throw new InvalidOperationException($"A análise da operação {operacaoId} sumiu depois do conflito.");
        }

        logger.LogInformation(
            "Operação {OperacaoId} da conta {ContaId}: {Decisao} com score {Score}",
            operacaoId,
            analise.ContaId,
            analise.Decisao,
            analise.ScoreFinal);
        return analise.ParaDto(caso);
    }

    private async Task<ResultadoAvaliacaoDto?> ExistenteAsync(Guid operacaoId, CancellationToken cancellationToken)
    {
        var analise = await analises.ObterPorOperacaoAsync(operacaoId, cancellationToken);
        return analise?.ParaDto(await casos.ObterPorOperacaoAsync(operacaoId, cancellationToken));
    }
}
