using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.ValueObjects;

namespace EuroBank.Antifraude.Domain.Repositories;

public interface IRepositorioListas
{
    /// <summary>As duas listas inteiras, como o motor as usa.</summary>
    Task<ListasRisco> ObterAsync(CancellationToken cancellationToken);

    Task<IbanBloqueado?> ObterIbanAsync(string iban, CancellationToken cancellationToken);

    Task<PaisRisco?> ObterPaisAsync(string codigo, CancellationToken cancellationToken);

    void Adicionar(IbanBloqueado iban);

    void Adicionar(PaisRisco pais);

    void Remover(IbanBloqueado iban);

    void Remover(PaisRisco pais);
}
