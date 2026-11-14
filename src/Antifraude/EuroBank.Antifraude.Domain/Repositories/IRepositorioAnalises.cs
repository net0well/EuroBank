using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.ValueObjects;

namespace EuroBank.Antifraude.Domain.Repositories;

public interface IRepositorioAnalises
{
    Task<Analise?> ObterAsync(AnaliseId id, CancellationToken cancellationToken);

    /// <summary>A análise da operação, se já houver (avaliar de novo devolve a mesma).</summary>
    Task<Analise?> ObterPorOperacaoAsync(Guid operacaoId, CancellationToken cancellationToken);

    void Adicionar(Analise analise);
}
