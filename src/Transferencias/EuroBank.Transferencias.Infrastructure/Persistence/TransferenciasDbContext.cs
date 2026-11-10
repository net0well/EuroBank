using EuroBank.Shared.Persistence.Outbox;
using EuroBank.Transferencias.Domain.Entities;
using EuroBank.Transferencias.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace EuroBank.Transferencias.Infrastructure.Persistence;

public sealed class TransferenciasDbContext(DbContextOptions<TransferenciasDbContext> options) : DbContext(options)
{
    public DbSet<Transferencia> Transferencias => Set<Transferencia>();

    public DbSet<MensagemOutbox> Outbox => Set<MensagemOutbox>();

    /// <summary>Projeção local das contas EuroBank (eventos ContaAberta de eurobank.contas.eventos).</summary>
    public DbSet<ContaConhecida> ContasConhecidas => Set<ContaConhecida>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TransferenciasDbContext).Assembly);
        modelBuilder.ApplyConfiguration(new MensagemOutboxConfiguration());
    }
}
