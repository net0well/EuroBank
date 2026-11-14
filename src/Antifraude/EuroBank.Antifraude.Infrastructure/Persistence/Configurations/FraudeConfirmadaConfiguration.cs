using EuroBank.Antifraude.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EuroBank.Antifraude.Infrastructure.Persistence.Configurations;

/// <summary>Tabela fraudes_confirmadas: uma por operação (os rótulos positivos do retreino).</summary>
public sealed class FraudeConfirmadaConfiguration : IEntityTypeConfiguration<FraudeConfirmada>
{
    public void Configure(EntityTypeBuilder<FraudeConfirmada> builder)
    {
        builder.ToTable("fraudes_confirmadas");

        builder.HasKey(fraude => fraude.Id);
        builder.Property(fraude => fraude.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(fraude => fraude.OperacaoId).HasColumnName("operacao_id");
        builder.HasIndex(fraude => fraude.OperacaoId).IsUnique();
        builder.Property(fraude => fraude.AnaliseId).HasColumnName("analise_id");
        builder.Property(fraude => fraude.Analista).HasColumnName("analista").HasMaxLength(100);
        builder.Property(fraude => fraude.Observacao).HasColumnName("observacao").HasMaxLength(1000);
        builder.Property(fraude => fraude.ConfirmadaEm).HasColumnName("confirmada_em");
    }
}
