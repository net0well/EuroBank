using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EuroBank.Antifraude.Infrastructure.Persistence.Repositories;

public sealed class RepositorioFraudes(AntifraudeDbContext contexto) : IRepositorioFraudes
{
    public Task<FraudeConfirmada?> ObterPorOperacaoAsync(Guid operacaoId, CancellationToken cancellationToken) =>
        contexto.FraudesConfirmadas.SingleOrDefaultAsync(fraude => fraude.OperacaoId == operacaoId, cancellationToken);

    public void Adicionar(FraudeConfirmada fraude) => contexto.FraudesConfirmadas.Add(fraude);
}
