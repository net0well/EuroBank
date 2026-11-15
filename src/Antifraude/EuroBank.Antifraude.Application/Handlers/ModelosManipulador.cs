using EuroBank.Antifraude.Application.Commands;
using EuroBank.Antifraude.Application.Interfaces;
using EuroBank.Antifraude.Application.Mappers;
using EuroBank.Antifraude.Application.Queries;
using EuroBank.Antifraude.Application.TransferObjects;
using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.Exceptions;
using EuroBank.Antifraude.Domain.Repositories;
using EuroBank.Shared.Domain;

namespace EuroBank.Antifraude.Application.Handlers;

/// <summary>
/// O registro de modelos: listar, detalhar, ativar, pôr em sombra e retirar o challenger. Trocas numa transação, a
/// versão que sai primeiro: os índices únicos não aceitam duas ativas (nem dois challengers) nem por um instante. Um
/// gatilho no banco avisa as instâncias da API (LISTEN/NOTIFY) quando a transação confirma.
/// </summary>
public sealed class ModelosManipulador(
    IRepositorioModelos modelos,
    IConsultaAntifraude consulta,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    IExecutorTransacional executor,
    TimeProvider relogio)
{
    public Task<IReadOnlyList<VersaoModeloResumoDto>> Handle(ListarModelos consultaModelos, CancellationToken cancellationToken) =>
        consulta.ListarModelosAsync(cancellationToken);

    public async Task<VersaoModeloDetalheDto> Handle(ObterModelo consultaModelo, CancellationToken cancellationToken) =>
        await consulta.ObterModeloAsync(consultaModelo.Versao, cancellationToken)
            ?? throw new ExcecaoModeloNaoEncontrado(consultaModelo.Versao);

    public async Task<VersaoModeloResumoDto> Handle(AtivarModelo comando, CancellationToken cancellationToken)
    {
        var alvo = await modelos.ObterAsync(comando.Versao, cancellationToken) ?? throw new ExcecaoModeloNaoEncontrado(comando.Versao);
        if (alvo.Status == StatusModelo.Ativa)
        {
            return alvo.ParaResumo();
        }

        var agora = relogio.GetUtcNow();
        await executor.ExecutarAsync(
            async token =>
            {
                if (await modelos.ComStatusAsync(StatusModelo.Ativa, token) is { } anterior)
                {
                    anterior.Aposentar(agora);
                    await unidadeDeTrabalho.SalvarAsync(token);
                }

                alvo.Ativar(agora);
                await unidadeDeTrabalho.SalvarAsync(token);
            },
            cancellationToken);
        return alvo.ParaResumo();
    }

    public async Task<VersaoModeloResumoDto> Handle(ColocarModeloEmSombra comando, CancellationToken cancellationToken)
    {
        var alvo = await modelos.ObterAsync(comando.Versao, cancellationToken) ?? throw new ExcecaoModeloNaoEncontrado(comando.Versao);
        if (alvo.Status == StatusModelo.Challenger)
        {
            return alvo.ParaResumo();
        }

        var agora = relogio.GetUtcNow();
        await executor.ExecutarAsync(
            async token =>
            {
                if (await modelos.ComStatusAsync(StatusModelo.Challenger, token) is { } anterior)
                {
                    anterior.Aposentar(agora);
                    await unidadeDeTrabalho.SalvarAsync(token);
                }

                // A ativa não vai para a sombra: a exceção desfaz a transação e o challenger anterior fica.
                alvo.ColocarEmSombra();
                await unidadeDeTrabalho.SalvarAsync(token);
            },
            cancellationToken);
        return alvo.ParaResumo();
    }

    public async Task Handle(RetirarChallenger comando, CancellationToken cancellationToken)
    {
        if (await modelos.ComStatusAsync(StatusModelo.Challenger, cancellationToken) is { } challenger)
        {
            challenger.Aposentar(relogio.GetUtcNow());
            await unidadeDeTrabalho.SalvarAsync(cancellationToken);
        }
    }
}
