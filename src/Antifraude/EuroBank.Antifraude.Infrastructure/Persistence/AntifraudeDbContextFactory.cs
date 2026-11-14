using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EuroBank.Antifraude.Infrastructure.Persistence;

/// <summary>Usado só pelo dotnet ef (migrations) em tempo de design.</summary>
public sealed class AntifraudeDbContextFactory : IDesignTimeDbContextFactory<AntifraudeDbContext>
{
    public AntifraudeDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Antifraude")
            ?? "Host=localhost;Port=5434;Database=eurobank_antifraude;Username=eurobank;Password=eurobank";

        return new AntifraudeDbContext(new DbContextOptionsBuilder<AntifraudeDbContext>().UseNpgsql(connectionString).Options);
    }
}
