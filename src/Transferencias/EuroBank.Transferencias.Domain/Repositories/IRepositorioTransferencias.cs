using EuroBank.Shared.Enums;
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

    /// <summary>Agendadas que chegaram na hora e passos com prazo vencido, das mais antigas para as mais novas.</summary>
    Task<IReadOnlyList<TransferenciaId>> ListarParaAvancarAsync(DateTimeOffset agora, int limite, CancellationToken cancellationToken);

    /// <summary>
    /// Trava a linha até o fim da transação se ela ainda estiver pendente. Devolve false se outra instância já a
    /// travou (SKIP LOCKED) ou se ela deixou de estar pendente desde a listagem.
    /// </summary>
    Task<bool> TravarParaAvancarAsync(TransferenciaId id, DateTimeOffset agora, CancellationToken cancellationToken);

    /// <summary>
    /// Soma o valor debitado (sem tarifa) das outras transferências da conta que já consomem o limite do dia, na
    /// moeda da conta.
    /// </summary>
    Task<Dinheiro> SomarConsumoDoLimiteAsync(
        Guid contaOrigemId,
        DateOnly dia,
        TransferenciaId exceto,
        Moeda moeda,
        CancellationToken cancellationToken);
}
