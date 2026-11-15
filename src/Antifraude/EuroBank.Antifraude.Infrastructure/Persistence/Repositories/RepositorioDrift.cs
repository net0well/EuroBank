using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EuroBank.Antifraude.Infrastructure.Persistence.Repositories;

public sealed class RepositorioDrift(AntifraudeDbContext contexto) : IRepositorioDrift
{
    public void Adicionar(MedicaoDrift medicao) => contexto.MedicoesDrift.Add(medicao);

    public Task<int> ApagarAnterioresAsync(DateTimeOffset limite, CancellationToken cancellationToken) =>
        contexto.MedicoesDrift.Where(medicao => medicao.MedidaEm < limite).ExecuteDeleteAsync(cancellationToken);
}
