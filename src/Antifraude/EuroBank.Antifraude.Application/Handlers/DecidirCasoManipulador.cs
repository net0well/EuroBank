using EuroBank.Antifraude.Application.Commands;
using EuroBank.Antifraude.Application.Extensions;
using EuroBank.Antifraude.Application.Interfaces;
using EuroBank.Antifraude.Application.TransferObjects;
using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Repositories;
using EuroBank.Shared.Domain;
using Microsoft.Extensions.Logging;

namespace EuroBank.Antifraude.Application.Handlers;

/// <summary>
/// A decisão do analista fecha o caso e a análise juntos (uma transação): a análise publica AnaliseDecidida pela
/// outbox e a saga segue (ou termina). Recusa com bloqueio põe o IBAN de destino na lista.
/// </summary>
public sealed class DecidirCasoManipulador(
    IRepositorioCasos casos,
    IRepositorioAnalises analises,
    IRepositorioListas listas,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    IConsultaAntifraude consulta,
    TimeProvider relogio,
    ILogger<DecidirCasoManipulador> logger)
{
    public async Task<CasoDetalheDto> Handle(DecidirCaso comando, CancellationToken cancellationToken)
    {
        var agora = relogio.GetUtcNow();
        var analista = comando.Analista?.Trim() ?? string.Empty;
        var caso = await casos.ObterOuFalharAsync(comando.CasoId, cancellationToken);
        var analise = await analises.ObterDoCasoAsync(caso, cancellationToken);

        caso.Decidir(analista, comando.Aprovar, comando.Justificativa, comando.BloquearIban, agora);
        analise.AplicarRevisao(comando.Aprovar, analista, comando.Justificativa.Trim(), agora);

        if (comando.BloquearIban && analise.Contexto.IbanDestino is { } iban
            && await listas.ObterIbanAsync(iban, cancellationToken) is null)
        {
            listas.Adicionar(IbanBloqueado.Criar(iban, $"Recusa do caso {caso.Id}: {comando.Justificativa.Trim()}", analista, agora));
        }

        await unidadeDeTrabalho.SalvarAsync(cancellationToken);
        logger.LogInformation(
            "Caso {CasoId} da operação {OperacaoId} {Decisao} por {Analista}",
            caso.Id,
            caso.OperacaoId,
            comando.Aprovar ? "aprovado" : "recusado",
            analista);
        return (await consulta.ObterCasoAsync(comando.CasoId, cancellationToken))!;
    }
}
