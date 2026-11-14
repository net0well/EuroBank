using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EuroBank.Notificacoes.Worker.Persistence;

/// <summary>Usado só pelo dotnet ef (migrations) em tempo de design.</summary>
public sealed class NotificacoesDbContextFactory : IDesignTimeDbContextFactory<NotificacoesDbContext>
{
    public NotificacoesDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Notificacoes")
            ?? "Host=localhost;Port=5434;Database=eurobank_notificacoes;Username=eurobank;Password=eurobank";

        return new NotificacoesDbContext(new DbContextOptionsBuilder<NotificacoesDbContext>().UseNpgsql(connectionString).Options);
    }
}
