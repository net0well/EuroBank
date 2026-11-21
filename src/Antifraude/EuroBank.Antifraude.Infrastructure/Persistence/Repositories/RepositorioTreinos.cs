using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EuroBank.Antifraude.Infrastructure.Persistence.Repositories;

public sealed class RepositorioTreinos(AntifraudeDbContext contexto) : IRepositorioTreinos
{
    public Task<ExecucaoTreino?> ObterAsync(Guid id, CancellationToken cancellationToken) =>
        contexto.ExecucoesTreino.SingleOrDefaultAsync(execucao => execucao.Id == id, cancellationToken);

    public Task<bool> ExisteAtivaAsync(CancellationToken cancellationToken) =>
        contexto.ExecucoesTreino.AnyAsync(
            execucao => execucao.Status == StatusTreino.Solicitada || execucao.Status == StatusTreino.EmAndamento,
            cancellationToken);

    public void Adicionar(ExecucaoTreino execucao) => contexto.ExecucoesTreino.Add(execucao);
}
