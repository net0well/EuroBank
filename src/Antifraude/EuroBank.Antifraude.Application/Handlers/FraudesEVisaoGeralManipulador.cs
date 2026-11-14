using EuroBank.Antifraude.Application.Commands;
using EuroBank.Antifraude.Application.Interfaces;
using EuroBank.Antifraude.Application.Mappers;
using EuroBank.Antifraude.Application.Queries;
using EuroBank.Antifraude.Application.TransferObjects;
using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Exceptions;
using EuroBank.Antifraude.Domain.Repositories;
using EuroBank.Shared.Domain;

namespace EuroBank.Antifraude.Application.Handlers;

/// <summary>Fraudes confirmadas (rótulos para o retreino) e a visão geral das últimas 24 horas.</summary>
public sealed class FraudesEVisaoGeralManipulador(
    IRepositorioAnalises analises,
    IRepositorioFraudes fraudes,
    IConsultaAntifraude consulta,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    TimeProvider relogio)
{
    public static readonly TimeSpan JanelaVisaoGeral = TimeSpan.FromHours(24);

    /// <summary>Só operação analisada; marcar de novo devolve a marcação original.</summary>
    public async Task<FraudeConfirmadaDto> Handle(ConfirmarFraude comando, CancellationToken cancellationToken)
    {
        var analise = await analises.ObterPorOperacaoAsync(comando.OperacaoId, cancellationToken)
            ?? throw new ExcecaoOperacaoNaoAnalisada(comando.OperacaoId);
        if (await fraudes.ObterPorOperacaoAsync(comando.OperacaoId, cancellationToken) is { } existente)
        {
            return existente.ParaDto();
        }

        var fraude = FraudeConfirmada.Registrar(analise, comando.Analista, comando.Observacao, relogio.GetUtcNow());
        fraudes.Adicionar(fraude);
        await unidadeDeTrabalho.SalvarAsync(cancellationToken);
        return fraude.ParaDto();
    }

    public Task<VisaoGeralDto> Handle(ObterVisaoGeral consultaVisao, CancellationToken cancellationToken) =>
        consulta.VisaoGeralAsync(relogio.GetUtcNow() - JanelaVisaoGeral, cancellationToken);
}
