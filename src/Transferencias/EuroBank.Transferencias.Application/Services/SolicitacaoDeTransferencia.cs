using EuroBank.Shared.Domain;
using EuroBank.Transferencias.Application.Exceptions;
using EuroBank.Transferencias.Application.Mappers;
using EuroBank.Transferencias.Application.TransferObjects;
using EuroBank.Transferencias.Domain.Entities;
using EuroBank.Transferencias.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace EuroBank.Transferencias.Application.Services;

/// <summary>
/// Caminho comum dos pedidos de transferência: Idempotency-Key, registro e, se for para já, o começo da saga na
/// mesma transação (o primeiro comando sai pela outbox junto). Repetir a mesma key com o mesmo corpo devolve a
/// transferência já criada; com outro corpo, 422.
/// </summary>
public sealed class SolicitacaoDeTransferencia(
    IRepositorioTransferencias repositorio,
    ServicoInicioTransferencia inicio,
    IExecutorTransacional executor,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    TimeProvider relogio,
    ILogger<SolicitacaoDeTransferencia> logger)
{
    public async Task<TransferenciaDto> RegistrarAsync(
        string? idempotencyKey,
        string hashDoPedido,
        Func<string, DateTimeOffset, Task<Transferencia>> criar,
        CancellationToken cancellationToken)
    {
        var chave = idempotencyKey?.Trim();
        if (string.IsNullOrEmpty(chave) || chave.Length > ExcecaoIdempotencyKeyObrigatoria.TamanhoMaximo)
        {
            throw new ExcecaoIdempotencyKeyObrigatoria();
        }

        var existente = await repositorio.ObterPorIdempotencyKeyAsync(chave, cancellationToken);
        if (existente is not null)
        {
            return existente.MesmoPedido(hashDoPedido) ? existente.ParaDto() : throw new ExcecaoIdempotencyKeyReutilizada();
        }

        var agora = relogio.GetUtcNow();
        var transferencia = await criar(chave, agora);
        repositorio.Adicionar(transferencia);

        await executor.ExecutarAsync(
            async token =>
            {
                await inicio.IniciarSeNaHoraAsync(transferencia, agora, token);
                await unidadeDeTrabalho.SalvarAsync(token);
            },
            cancellationToken);

        logger.LogInformation(
            "Transferência {Tipo} {TransferenciaId} da conta {ContaId}: {Estado}",
            transferencia.Tipo, transferencia.Id, transferencia.ContaOrigemId, transferencia.Estado);
        return transferencia.ParaDto();
    }
}
