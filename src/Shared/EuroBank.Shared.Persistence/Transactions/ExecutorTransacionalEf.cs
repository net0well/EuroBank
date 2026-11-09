using EuroBank.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace EuroBank.Shared.Persistence.Transactions;

/// <summary>Transação do EF em volta da operação; se já houver uma aberta, a operação só participa dela.</summary>
public sealed class ExecutorTransacionalEf<TContexto>(TContexto contexto) : IExecutorTransacional
    where TContexto : DbContext
{
    public async Task ExecutarAsync(Func<CancellationToken, Task> operacao, CancellationToken cancellationToken)
    {
        if (contexto.Database.CurrentTransaction is not null)
        {
            await operacao(cancellationToken);
            return;
        }

        await using var transacao = await contexto.Database.BeginTransactionAsync(cancellationToken);
        await operacao(cancellationToken);
        await transacao.CommitAsync(cancellationToken);
    }
}
