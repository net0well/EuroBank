using EuroBank.Transferencias.Domain.Entities;
using EuroBank.Transferencias.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EuroBank.Transferencias.Infrastructure.Persistence.Configurations;

/// <summary>Histórico da saga: uma linha por passo, chave (transferência, ordem).</summary>
public sealed class PassoTransferenciaConfiguration : IEntityTypeConfiguration<PassoTransferencia>
{
    public void Configure(EntityTypeBuilder<PassoTransferencia> builder)
    {
        builder.ToTable("passos_transferencia");

        builder.Property<TransferenciaId>("TransferenciaId")
            .HasColumnName("transferencia_id")
            .HasConversion(id => id.Valor, valor => new TransferenciaId(valor));

        // Chave não gerada pelo banco: um passo novo na coleção vira INSERT.
        builder.HasKey("TransferenciaId", nameof(PassoTransferencia.Ordem));
        builder.Property(passo => passo.Ordem).HasColumnName("ordem").ValueGeneratedNever();
        builder.Property(passo => passo.EstadoApos).HasColumnName("estado_apos").HasConversion<string>().HasMaxLength(30);
        builder.Property(passo => passo.Descricao).HasColumnName("descricao").HasMaxLength(300);
        builder.Property(passo => passo.OcorridoEm).HasColumnName("ocorrido_em");
    }
}
