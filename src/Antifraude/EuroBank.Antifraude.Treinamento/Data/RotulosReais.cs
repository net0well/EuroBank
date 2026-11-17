using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.ValueObjects;
using EuroBank.Antifraude.Infrastructure.Persistence;
using EuroBank.Antifraude.Treinamento.Training;
using Microsoft.EntityFrameworkCore;

namespace EuroBank.Antifraude.Treinamento.Data;

/// <summary>
/// Os rótulos que o laboratório produziu: análises com as features guardadas que um analista decidiu (recusa é fraude,
/// aprovação não é) ou que foram confirmadas como fraude depois (sempre fraude). Expiradas, canceladas e as decididas
/// só pelo motor não têm rótulo: ninguém disse se eram fraude.
/// </summary>
public static class RotulosReais
{
    public static async Task<IReadOnlyList<LinhaTreino>> CarregarAsync(AntifraudeDbContext contexto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        var confirmadas = (await contexto.FraudesConfirmadas.AsNoTracking()
                .Select(fraude => fraude.AnaliseId)
                .ToListAsync(cancellationToken))
            .Select(id => new AnaliseId(id))
            .ToHashSet();

        var analises = await contexto.Analises.AsNoTracking()
            .Where(analise => analise.Features != null && (analise.Analista != null || confirmadas.Contains(analise.Id)))
            .ToListAsync(cancellationToken);

        return analises
            .Select(analise => new LinhaTreino(
                analise.CriadaEm,
                analise.Features!,
                Fraude: confirmadas.Contains(analise.Id) || analise.Decisao == DecisaoAnalise.Recusada,
                Real: true))
            .ToList();
    }
}
