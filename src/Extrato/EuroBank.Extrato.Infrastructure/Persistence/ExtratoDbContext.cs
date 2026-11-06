using EuroBank.Extrato.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EuroBank.Extrato.Infrastructure.Persistence;

/// <summary>Database eurobank_extrato: a projeção. As tabelas do Wolverine (inbox, dead letter) ficam no schema wolverine.</summary>
public sealed class ExtratoDbContext(DbContextOptions<ExtratoDbContext> options) : DbContext(options)
{
    public DbSet<ExtratoConta> Extratos => Set<ExtratoConta>();

    public DbSet<Lancamento> Lancamentos => Set<Lancamento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ExtratoDbContext).Assembly);
}
