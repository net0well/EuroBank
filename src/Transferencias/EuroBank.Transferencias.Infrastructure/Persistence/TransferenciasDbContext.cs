using EuroBank.Shared.Persistence.Outbox;
using EuroBank.Transferencias.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EuroBank.Transferencias.Infrastructure.Persistence;

public sealed class TransferenciasDbContext(DbContextOptions<TransferenciasDbContext> options) : DbContext(options)
{
    public DbSet<Transferencia> Transferencias => Set<Transferencia>();

    public DbSet<MensagemOutbox> Outbox => Set<MensagemOutbox>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TransferenciasDbContext).Assembly);
        modelBuilder.ApplyConfiguration(new MensagemOutboxConfiguration());
    }
}
