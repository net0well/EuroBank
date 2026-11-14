using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Enums;

namespace EuroBank.Antifraude.Domain.Repositories;

public interface IRepositorioRegras
{
    Task<IReadOnlyList<Regra>> ListarAsync(CancellationToken cancellationToken);

    Task<Regra?> ObterAsync(CodigoRegra codigo, CancellationToken cancellationToken);

    void Adicionar(Regra regra);
}
