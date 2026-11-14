using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Exceptions;
using EuroBank.Antifraude.Domain.Repositories;
using EuroBank.Antifraude.Domain.ValueObjects;

namespace EuroBank.Antifraude.Application.Extensions;

public static class RepositoriosExtensions
{
    /// <exception cref="ExcecaoCasoNaoEncontrado">Quando o caso não existe (vira 404).</exception>
    public static async Task<CasoRevisao> ObterOuFalharAsync(this IRepositorioCasos casos, Guid casoId, CancellationToken cancellationToken) =>
        await casos.ObterAsync(new CasoId(casoId), cancellationToken) ?? throw new ExcecaoCasoNaoEncontrado(casoId);

    /// <summary>A análise do caso: as duas nascem juntas, então a falta dela é inconsistência, não 404.</summary>
    public static async Task<Analise> ObterDoCasoAsync(this IRepositorioAnalises analises, CasoRevisao caso, CancellationToken cancellationToken) =>
        await analises.ObterAsync(caso.AnaliseId, cancellationToken)
        ?? throw new InvalidOperationException($"O caso {caso.Id} aponta para a análise {caso.AnaliseId}, que não existe.");
}
