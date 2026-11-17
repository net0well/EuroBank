using EuroBank.Antifraude.Domain.Entities;

namespace EuroBank.Antifraude.Domain.Repositories;

public interface IRepositorioDrift
{
    void Adicionar(MedicaoDrift medicao);

    /// <summary>Apaga as medições feitas antes do limite; devolve quantas.</summary>
    Task<int> ApagarAnterioresAsync(DateTimeOffset limite, CancellationToken cancellationToken);
}
