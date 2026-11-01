using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EuroBank.Contas.Infrastructure.Persistence;

/// <summary>Usado só pelo dotnet ef (migrations) em tempo de design.</summary>
public sealed class ContasDbContextFactory : IDesignTimeDbContextFactory<ContasDbContext>
{
    public ContasDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Contas")
            ?? "Host=localhost;Port=5434;Database=eurobank_contas;Username=eurobank;Password=eurobank";

        return new ContasDbContext(new DbContextOptionsBuilder<ContasDbContext>().UseNpgsql(connectionString).Options);
    }
}
