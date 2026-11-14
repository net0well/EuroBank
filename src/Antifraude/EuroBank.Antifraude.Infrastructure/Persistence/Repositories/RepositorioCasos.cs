using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.Repositories;
using EuroBank.Antifraude.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace EuroBank.Antifraude.Infrastructure.Persistence.Repositories;

public sealed class RepositorioCasos(AntifraudeDbContext contexto) : IRepositorioCasos
{
    public Task<CasoRevisao?> ObterAsync(CasoId id, CancellationToken cancellationToken) =>
        contexto.Casos.SingleOrDefaultAsync(caso => caso.Id == id, cancellationToken);

    public Task<CasoRevisao?> ObterPorOperacaoAsync(Guid operacaoId, CancellationToken cancellationToken) =>
        contexto.Casos.SingleOrDefaultAsync(caso => caso.OperacaoId == operacaoId, cancellationToken);

    /// <summary>
    /// Sem trava de linha: duas instâncias do worker podem pegar o mesmo caso, e a versão otimista faz uma delas
    /// perder (o caso já estará expirado na nova tentativa).
    /// </summary>
    public async Task<IReadOnlyList<CasoRevisao>> VencidosAsync(DateTimeOffset agora, int limite, CancellationToken cancellationToken) =>
        await contexto.Casos
            .Where(caso => (caso.Estado == EstadoCaso.Aberto || caso.Estado == EstadoCaso.EmAnalise) && caso.ExpiraEm <= agora)
            .OrderBy(caso => caso.ExpiraEm)
            .Take(limite)
            .ToListAsync(cancellationToken);

    public void Adicionar(CasoRevisao caso) => contexto.Casos.Add(caso);
}
