using EuroBank.Antifraude.Domain.Entities;

namespace EuroBank.Antifraude.Domain.Repositories;

public interface IRepositorioFraudes
{
    Task<FraudeConfirmada?> ObterPorOperacaoAsync(Guid operacaoId, CancellationToken cancellationToken);

    void Adicionar(FraudeConfirmada fraude);
}
