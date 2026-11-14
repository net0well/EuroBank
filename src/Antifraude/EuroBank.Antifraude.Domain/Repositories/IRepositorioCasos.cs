using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.ValueObjects;

namespace EuroBank.Antifraude.Domain.Repositories;

public interface IRepositorioCasos
{
    Task<CasoRevisao?> ObterAsync(CasoId id, CancellationToken cancellationToken);

    Task<CasoRevisao?> ObterPorOperacaoAsync(Guid operacaoId, CancellationToken cancellationToken);

    /// <summary>Casos ainda pendentes cujo prazo já passou, dos mais antigos para os mais novos.</summary>
    Task<IReadOnlyList<CasoRevisao>> VencidosAsync(DateTimeOffset agora, int limite, CancellationToken cancellationToken);

    void Adicionar(CasoRevisao caso);
}
