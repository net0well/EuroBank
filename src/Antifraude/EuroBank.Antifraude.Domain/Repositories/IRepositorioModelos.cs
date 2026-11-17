using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Enums;

namespace EuroBank.Antifraude.Domain.Repositories;

public interface IRepositorioModelos
{
    Task<VersaoModelo?> ObterAsync(int versao, CancellationToken cancellationToken);

    /// <summary>A versão no status pedido (Ativa ou Challenger: há no máximo uma de cada).</summary>
    Task<VersaoModelo?> ComStatusAsync(StatusModelo status, CancellationToken cancellationToken);

    void Adicionar(VersaoModelo versao);
}
