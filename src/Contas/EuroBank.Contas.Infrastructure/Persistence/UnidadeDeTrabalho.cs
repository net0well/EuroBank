using EuroBank.Contas.Infrastructure.Outbox;
using EuroBank.Shared.Domain;
using EuroBank.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EuroBank.Contas.Infrastructure.Persistence;

public sealed class UnidadeDeTrabalho(ContasDbContext contexto, IOptions<OpcoesOutbox> opcoes) : IUnidadeDeTrabalho
{
    public async Task SalvarAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (opcoes.Value.ApagarAposGravar)
            {
                await SalvarEApagarOutboxAsync(cancellationToken);
            }
            else
            {
                await contexto.SaveChangesAsync(cancellationToken);
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ExcecaoConcorrencia();
        }
    }

    /// <summary>
    /// INSERT e DELETE da outbox na mesma transação: a tabela fica vazia, mas o INSERT já está no WAL e o
    /// Debezium publica o evento do mesmo jeito (o DELETE é ignorado pelo Outbox Event Router).
    /// </summary>
    private async Task SalvarEApagarOutboxAsync(CancellationToken cancellationToken)
    {
        await using var transacao = await contexto.Database.BeginTransactionAsync(cancellationToken);

        await contexto.SaveChangesAsync(cancellationToken);

        var entradas = contexto.ChangeTracker.Entries<MensagemOutbox>().ToList();
        var ids = entradas.Select(entrada => entrada.Entity.Id).ToList();
        if (ids.Count > 0)
        {
            await contexto.Outbox.Where(mensagem => ids.Contains(mensagem.Id)).ExecuteDeleteAsync(cancellationToken);
        }

        await transacao.CommitAsync(cancellationToken);

        foreach (var entrada in entradas)
        {
            entrada.State = EntityState.Detached;
        }
    }
}
