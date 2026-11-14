using EuroBank.Antifraude.Domain.Entities;

namespace EuroBank.Antifraude.Domain.Repositories;

public interface IRepositorioConfiguracao
{
    /// <summary>A versão mais recente; nula só antes dos dados iniciais.</summary>
    Task<ConfiguracaoDecisao?> AtualAsync(CancellationToken cancellationToken);

    void Adicionar(ConfiguracaoDecisao configuracao);
}
