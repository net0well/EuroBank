using EuroBank.Shared.Enums;
using EuroBank.Shared.ValueObjects;
using EuroBank.Transferencias.Domain.Entities;
using EuroBank.Transferencias.Domain.Enums;
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

    public async Task<IReadOnlyList<TransferenciaId>> ListarParaAvancarAsync(
        DateTimeOffset agora,
        int limite,
        CancellationToken cancellationToken) =>
        await contexto.Transferencias
            .AsNoTracking()
            .IgnoreAutoIncludes()
            .Where(transferencia =>
                (transferencia.Estado == EstadoTransferencia.Agendada && transferencia.ExecutarEmEfetivo <= agora)
                || transferencia.PrazoDoPasso <= agora)
            .OrderBy(transferencia => transferencia.AtualizadaEm)
            .Select(transferencia => transferencia.Id)
            .Take(limite)
            .ToListAsync(cancellationToken);

    public async Task<bool> TravarParaAvancarAsync(TransferenciaId id, DateTimeOffset agora, CancellationToken cancellationToken)
    {
        var travadas = await contexto.Database
            .SqlQuery<Guid>(
                $"""
                SELECT id AS "Value" FROM transferencias
                WHERE id = {id.Valor}
                  AND ((estado = 'Agendada' AND executar_em_efetivo <= {agora}) OR prazo_do_passo <= {agora})
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);
        return travadas.Count == 1;
    }

    public async Task<Dinheiro> SomarConsumoDoLimiteAsync(
        Guid contaOrigemId,
        DateOnly dia,
        TransferenciaId exceto,
        Moeda moeda,
        CancellationToken cancellationToken)
    {
        var inicio = new DateTimeOffset(dia.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var fim = inicio.AddDays(1);
        var estados = TabelaTransferencias.EstadosQueConsomemLimite;

        // O que sai da conta, sem a tarifa: na moeda da conta, então somar é seguro.
        var soma = await contexto.Transferencias
            .AsNoTracking()
            .IgnoreAutoIncludes()
            .Where(transferencia => transferencia.ContaOrigemId == contaOrigemId
                && transferencia.Id != exceto
                && transferencia.ExecutarEmEfetivo >= inicio
                && transferencia.ExecutarEmEfetivo < fim
                && estados.Contains(transferencia.Estado))
            .SumAsync(transferencia => EF.Property<decimal?>(transferencia, "_valorDebitado") ?? 0m, cancellationToken);

        return Dinheiro.De(soma, moeda);
    }
}
