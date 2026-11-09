using EuroBank.Shared.ValueObjects;
using EuroBank.Transferencias.Domain.Entities;
using EuroBank.Transferencias.Domain.Policies;
using EuroBank.Transferencias.Domain.Repositories;
using EuroBank.Transferencias.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace EuroBank.Transferencias.Infrastructure.Persistence.Repositories;

public sealed class RepositorioTransferencias(TransferenciasDbContext contexto) : IRepositorioTransferencias
{
    public Task<Transferencia?> ObterPorIdAsync(TransferenciaId id, CancellationToken cancellationToken) =>
        contexto.Transferencias.FirstOrDefaultAsync(transferencia => transferencia.Id == id, cancellationToken);

    public Task<Transferencia?> ObterPorIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken) =>
        contexto.Transferencias.FirstOrDefaultAsync(
            transferencia => transferencia.IdempotencyKey == idempotencyKey, cancellationToken);

    public void Adicionar(Transferencia transferencia) => contexto.Transferencias.Add(transferencia);

    /// <summary>
    /// Advisory lock do Postgres com a chave derivada do id da conta. É liberado sozinho no commit ou no rollback
    /// (xact) e não trava nenhuma linha: só faz quem chega depois esperar a vez.
    /// </summary>
    public Task TravarLimiteDaContaAsync(Guid contaOrigemId, CancellationToken cancellationToken) =>
        contexto.Database.ExecuteSqlAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({contaOrigemId.ToString()}, 0))", cancellationToken);

    public async Task<Dinheiro> SomarConsumoDoLimiteAsync(
        Guid contaOrigemId,
        DateOnly dia,
        TransferenciaId exceto,
        CancellationToken cancellationToken)
    {
        var inicio = new DateTimeOffset(dia.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var fim = inicio.AddDays(1);
        var estados = TabelaTransferencias.EstadosQueConsomemLimite;

        var valores = await contexto.Transferencias
            .AsNoTracking()
            .Where(transferencia => transferencia.ContaOrigemId == contaOrigemId
                && transferencia.Id != exceto
                && transferencia.ExecutarEmEfetivo >= inicio
                && transferencia.ExecutarEmEfetivo < fim
                && estados.Contains(transferencia.Estado))
            .Select(transferencia => transferencia.Valor)
            .ToListAsync(cancellationToken);

        return valores.Aggregate(Dinheiro.Zero, (soma, valor) => soma + valor);
    }
}
