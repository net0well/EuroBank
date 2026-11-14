using EuroBank.Notificacoes.Worker.Entities;
using Microsoft.EntityFrameworkCore;

namespace EuroBank.Notificacoes.Worker.Persistence;

/// <summary>Database eurobank_notificacoes: os e-mails enviados. As tabelas do Wolverine ficam no schema wolverine.</summary>
public sealed class NotificacoesDbContext(DbContextOptions<NotificacoesDbContext> options) : DbContext(options)
{
    public DbSet<Notificacao> Notificacoes => Set<Notificacao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificacoesDbContext).Assembly);
}
