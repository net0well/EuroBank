using EuroBank.Shared.Domain;
using EuroBank.Transferencias.Domain.Entities;
using EuroBank.Transferencias.Domain.Enums;
using EuroBank.Transferencias.Domain.Repositories;
using EuroBank.Transferencias.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace EuroBank.Transferencias.Application.Services;

/// <summary>
/// Caminho comum de toda resposta: carrega a saga, deixa o agregado decidir (aplicar ou ignorar) e grava o
/// novo estado com os próximos comandos na outbox. Conflito de versão sobe como ExcecaoConcorrencia e o
/// Wolverine tenta de novo com a saga recarregada.
/// </summary>
public sealed class AplicadorRespostas(
    IRepositorioTransferencias repositorio,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    TimeProvider relogio,
    ILogger<AplicadorRespostas> logger)
{
    public async Task AplicarAsync(
        Guid transferenciaId,
        string resposta,
        Func<Transferencia, DateTimeOffset, ResultadoPasso> aplicar,
        CancellationToken cancellationToken)
    {
        var transferencia = await repositorio.ObterPorIdAsync(new TransferenciaId(transferenciaId), cancellationToken);
        if (transferencia is null)
        {
            // Resposta de uma transferência que este banco não conhece (ex.: comando enviado à mão no laboratório).
            logger.LogWarning("{Resposta} da transferência desconhecida {TransferenciaId} ignorada", resposta, transferenciaId);
            return;
        }

        var resultado = aplicar(transferencia, relogio.GetUtcNow());
        await unidadeDeTrabalho.SalvarAsync(cancellationToken);

        logger.LogInformation(
            "{Resposta} da transferência {TransferenciaId}: {Resultado}, estado {Estado}",
            resposta, transferenciaId, resultado, transferencia.Estado);
    }
}
