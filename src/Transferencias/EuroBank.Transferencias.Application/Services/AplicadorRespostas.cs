using EuroBank.Shared.Domain;
using EuroBank.Shared.ValueObjects;
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
    IExecutorTransacional executor,
    ServicoInicioTransferencia inicio,
    TimeProvider relogio,
    ILogger<AplicadorRespostas> logger)
{
    public Task AplicarAsync(
        Guid transferenciaId,
        string resposta,
        Func<Transferencia, DateTimeOffset, ResultadoPasso> aplicar,
        CancellationToken cancellationToken) =>
        AplicarComLimiteAsync(
            transferenciaId,
            resposta,
            consultaLimite: false,
            (transferencia, _, agora) => aplicar(transferencia, agora),
            cancellationToken);

    /// <summary>
    /// Para a resposta que decide o débito (a cotação): trava a conta e soma o que ela já transferiu no dia, na
    /// mesma transação em que a saga pede o débito.
    /// </summary>
    public Task AplicarComLimiteAsync(
        Guid transferenciaId,
        string resposta,
        Func<Transferencia, Dinheiro, DateTimeOffset, ResultadoPasso> aplicar,
        CancellationToken cancellationToken) =>
        AplicarComLimiteAsync(transferenciaId, resposta, consultaLimite: true, aplicar, cancellationToken);

    private Task AplicarComLimiteAsync(
        Guid transferenciaId,
        string resposta,
        bool consultaLimite,
        Func<Transferencia, Dinheiro, DateTimeOffset, ResultadoPasso> aplicar,
        CancellationToken cancellationToken) =>
        executor.ExecutarAsync(
            async token =>
            {
                var transferencia = await repositorio.ObterPorIdAsync(new TransferenciaId(transferenciaId), token);
                if (transferencia is null)
                {
                    // Resposta de uma transferência que este banco não conhece (ex.: comando enviado à mão no laboratório).
                    logger.LogWarning("{Resposta} da transferência desconhecida {TransferenciaId} ignorada", resposta, transferenciaId);
                    return;
                }

                var jaTransferido = consultaLimite
                    ? await inicio.ConsumoDoDiaAsync(transferencia, token)
                    : Dinheiro.ZeroEm(transferencia.MoedaOrigem);
                var resultado = aplicar(transferencia, jaTransferido, relogio.GetUtcNow());
                await unidadeDeTrabalho.SalvarAsync(token);

                logger.LogInformation(
                    "{Resposta} da transferência {TransferenciaId}: {Resultado}, estado {Estado}",
                    resposta, transferenciaId, resultado, transferencia.Estado);
            },
            cancellationToken);
}
