using EuroBank.Antifraude.Domain.Entities;

namespace EuroBank.Antifraude.Domain.Repositories;

public interface IRepositorioTreinos
{
    Task<ExecucaoTreino?> ObterAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Se há uma execução Solicitada ou EmAndamento.</summary>
    Task<bool> ExisteAtivaAsync(CancellationToken cancellationToken);

    void Adicionar(ExecucaoTreino execucao);
}
