using EuroBank.Extrato.Domain.Entities;

namespace EuroBank.Extrato.Domain.Repositories;

public interface IRepositorioExtratos
{
    Task<ExtratoConta?> ObterAsync(Guid contaId, CancellationToken cancellationToken);

    void Adicionar(ExtratoConta extrato);
}
