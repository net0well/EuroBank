using EuroBank.Shared.Domain;
using EuroBank.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace EuroBank.Extrato.Infrastructure.Persistence;

public sealed class UnidadeDeTrabalho(ExtratoDbContext contexto) : IUnidadeDeTrabalho
{
    public async Task SalvarAsync(CancellationToken cancellationToken)
    {
        try
        {
            await contexto.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ExcecaoConcorrencia();
        }
    }
}
