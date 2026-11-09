using EuroBank.Shared.ValueObjects;
using EuroBank.Transferencias.Domain.Entities;
using EuroBank.Transferencias.Domain.ValueObjects;

namespace EuroBank.Transferencias.Domain.Repositories;

public interface IRepositorioTransferencias
{
    Task<Transferencia?> ObterPorIdAsync(TransferenciaId id, CancellationToken cancellationToken);

    Task<Transferencia?> ObterPorIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken);

    void Adicionar(Transferencia transferencia);

    /// <summary>
    /// Serializa o início das transferências da mesma conta até o fim da transação atual. Sem isso, duas
    /// transferências começando juntas somariam o limite sem ver uma à outra.
    /// </summary>
    Task TravarLimiteDaContaAsync(Guid contaOrigemId, CancellationToken cancellationToken);

    /// <summary>Soma o valor (sem tarifa) das outras transferências da conta que já consomem o limite do dia.</summary>
    Task<Dinheiro> SomarConsumoDoLimiteAsync(
        Guid contaOrigemId,
        DateOnly dia,
        TransferenciaId exceto,
        CancellationToken cancellationToken);
}
