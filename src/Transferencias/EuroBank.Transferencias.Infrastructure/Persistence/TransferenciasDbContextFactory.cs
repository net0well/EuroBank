using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EuroBank.Transferencias.Infrastructure.Persistence;

/// <summary>Usado só pelo dotnet ef (migrations) em tempo de design.</summary>
public sealed class TransferenciasDbContextFactory : IDesignTimeDbContextFactory<TransferenciasDbContext>
{
    public TransferenciasDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Transferencias")
            ?? "Host=localhost;Port=5434;Database=eurobank_transferencias;Username=eurobank;Password=eurobank";

        return new TransferenciasDbContext(
            new DbContextOptionsBuilder<TransferenciasDbContext>().UseNpgsql(connectionString).Options);
    }
}
