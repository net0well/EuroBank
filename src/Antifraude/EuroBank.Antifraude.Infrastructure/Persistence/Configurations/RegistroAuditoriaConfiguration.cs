using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EuroBank.Antifraude.Infrastructure.Persistence.Configurations;

/// <summary>Auditoria do caso: uma linha por ação, chave (caso, ordem).</summary>
public sealed class RegistroAuditoriaConfiguration : IEntityTypeConfiguration<RegistroAuditoria>
{
    public void Configure(EntityTypeBuilder<RegistroAuditoria> builder)
    {
        builder.ToTable("casos_revisao_auditoria");

        builder.Property<CasoId>("CasoId")
            .HasColumnName("caso_id")
            .HasConversion(id => id.Valor, valor => new CasoId(valor));

        // Chave não gerada pelo banco: uma ação nova na coleção vira INSERT.
        builder.HasKey("CasoId", nameof(RegistroAuditoria.Ordem));
        builder.Property(registro => registro.Ordem).HasColumnName("ordem").ValueGeneratedNever();
        builder.Property(registro => registro.Quando).HasColumnName("quando");
        builder.Property(registro => registro.Quem).HasColumnName("quem").HasMaxLength(100);
        builder.Property(registro => registro.Acao).HasColumnName("acao").HasMaxLength(60);
        builder.Property(registro => registro.Detalhe).HasColumnName("detalhe").HasMaxLength(1100);
    }
}
