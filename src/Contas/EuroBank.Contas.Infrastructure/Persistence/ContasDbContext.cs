using EuroBank.Contas.Domain.Entities;
using EuroBank.Contas.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;

namespace EuroBank.Contas.Infrastructure.Persistence;

public sealed class ContasDbContext(DbContextOptions<ContasDbContext> options) : DbContext(options)
{
    /// <summary>Sequence que numera as contas; o número entra no IBAN.</summary>
    public const string SequenciaNumeroConta = "seq_numero_conta";

    public DbSet<Conta> Contas => Set<Conta>();

    public DbSet<MensagemOutbox> Outbox => Set<MensagemOutbox>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasSequence<long>(SequenciaNumeroConta).StartsAt(1);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ContasDbContext).Assembly);
    }
}
