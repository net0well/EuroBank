using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EuroBank.Extrato.Infrastructure.Persistence;

/// <summary>Usado só pelo dotnet ef (migrations) em tempo de design.</summary>
public sealed class ExtratoDbContextFactory : IDesignTimeDbContextFactory<ExtratoDbContext>
{
    public ExtratoDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Extrato")
            ?? "Host=localhost;Port=5434;Database=eurobank_extrato;Username=eurobank;Password=eurobank";

        return new ExtratoDbContext(new DbContextOptionsBuilder<ExtratoDbContext>().UseNpgsql(connectionString).Options);
    }
}
