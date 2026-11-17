using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EuroBank.Antifraude.Infrastructure.Persistence.Repositories;

public sealed class RepositorioModelos(AntifraudeDbContext contexto) : IRepositorioModelos
{
    public Task<VersaoModelo?> ObterAsync(int versao, CancellationToken cancellationToken) =>
        contexto.VersoesModelo.SingleOrDefaultAsync(modelo => modelo.Versao == versao, cancellationToken);

    public Task<VersaoModelo?> ComStatusAsync(StatusModelo status, CancellationToken cancellationToken) =>
        contexto.VersoesModelo.SingleOrDefaultAsync(modelo => modelo.Status == status, cancellationToken);

    public void Adicionar(VersaoModelo versao) => contexto.VersoesModelo.Add(versao);
}
