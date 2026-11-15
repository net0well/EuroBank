using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Shared.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace EuroBank.Antifraude.Infrastructure.Persistence;

/// <summary>
/// Database eurobank_antifraude: análises, casos de revisão, regras, configuração de decisão, listas e a outbox
/// (eurobank.antifraude.eventos). O perfil de comportamento das contas fica no Redis, não aqui.
/// </summary>
public sealed class AntifraudeDbContext(DbContextOptions<AntifraudeDbContext> options) : DbContext(options)
{
    public DbSet<Analise> Analises => Set<Analise>();

    public DbSet<CasoRevisao> Casos => Set<CasoRevisao>();

    public DbSet<Regra> Regras => Set<Regra>();

    public DbSet<ConfiguracaoDecisao> Configuracoes => Set<ConfiguracaoDecisao>();

    public DbSet<IbanBloqueado> IbansBloqueados => Set<IbanBloqueado>();

    public DbSet<PaisRisco> PaisesRisco => Set<PaisRisco>();

    public DbSet<FraudeConfirmada> FraudesConfirmadas => Set<FraudeConfirmada>();

    public DbSet<VersaoModelo> VersoesModelo => Set<VersaoModelo>();

    public DbSet<MensagemOutbox> Outbox => Set<MensagemOutbox>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AntifraudeDbContext).Assembly);
        modelBuilder.ApplyConfiguration(new MensagemOutboxConfiguration());
    }
}
