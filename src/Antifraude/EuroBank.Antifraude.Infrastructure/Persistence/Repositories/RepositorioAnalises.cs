using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Repositories;
using EuroBank.Antifraude.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace EuroBank.Antifraude.Infrastructure.Persistence.Repositories;

public sealed class RepositorioAnalises(AntifraudeDbContext contexto) : IRepositorioAnalises
{
    public Task<Analise?> ObterAsync(AnaliseId id, CancellationToken cancellationToken) =>
        contexto.Analises.SingleOrDefaultAsync(analise => analise.Id == id, cancellationToken);

    public Task<Analise?> ObterPorOperacaoAsync(Guid operacaoId, CancellationToken cancellationToken) =>
        contexto.Analises.SingleOrDefaultAsync(analise => analise.OperacaoId == operacaoId, cancellationToken);

    public void Adicionar(Analise analise) => contexto.Analises.Add(analise);
}
