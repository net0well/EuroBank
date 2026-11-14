using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EuroBank.Antifraude.Infrastructure.Persistence.Repositories;

public sealed class RepositorioRegras(AntifraudeDbContext contexto) : IRepositorioRegras
{
    public async Task<IReadOnlyList<Regra>> ListarAsync(CancellationToken cancellationToken) =>
        await contexto.Regras.OrderBy(regra => regra.Codigo).ToListAsync(cancellationToken);

    public Task<Regra?> ObterAsync(CodigoRegra codigo, CancellationToken cancellationToken) =>
        contexto.Regras.SingleOrDefaultAsync(regra => regra.Codigo == codigo, cancellationToken);

    public void Adicionar(Regra regra) => contexto.Regras.Add(regra);
}
