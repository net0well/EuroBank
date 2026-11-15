using EuroBank.Antifraude.Application.Interfaces;
using EuroBank.Antifraude.Application.TransferObjects;
using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace EuroBank.Antifraude.Infrastructure.Persistence.Queries;

/// <summary>Só as colunas que o drift usa: nada do arquivo do modelo, nada dos sinais das análises.</summary>
public sealed class ConsultaDrift(AntifraudeDbContext contexto) : IConsultaDrift
{
    public async Task<ReferenciaDrift?> ReferenciaAtivaAsync(CancellationToken cancellationToken)
    {
        var ativa = await contexto.VersoesModelo.AsNoTracking()
            .Where(versao => versao.Status == StatusModelo.Ativa)
            .Select(versao => new { versao.Versao, Distribuicao = EF.Property<List<DistribuicaoFeature>>(versao, "_distribuicao") })
            .SingleOrDefaultAsync(cancellationToken);
        return ativa is null ? null : new ReferenciaDrift(ativa.Versao, ativa.Distribuicao);
    }

    public async Task<AmostraDrift> AmostraAsync(
        int versao, DateTimeOffset inicio, DateTimeOffset fim, int maximo, CancellationToken cancellationToken)
    {
        var analises = await contexto.Analises.AsNoTracking()
            .Where(analise => analise.CriadaEm > inicio && analise.CriadaEm <= fim && analise.Features != null)
            .OrderByDescending(analise => analise.CriadaEm)
            .Take(maximo)
            .Select(analise => new { analise.Features, analise.Modelo })
            .ToListAsync(cancellationToken);

        return new AmostraDrift(
            analises.Select(analise => analise.Features!).ToList(),
            analises
                .Where(analise => analise.Modelo is not null && analise.Modelo.Versao == versao)
                .Select(analise => (double)analise.Modelo!.Probabilidade)
                .ToList());
    }
}
