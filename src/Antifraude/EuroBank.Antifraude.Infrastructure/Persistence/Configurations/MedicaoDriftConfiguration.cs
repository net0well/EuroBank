using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.ValueObjects;
using EuroBank.Antifraude.Infrastructure.Persistence.Conversions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EuroBank.Antifraude.Infrastructure.Persistence.Configurations;

/// <summary>Tabela medicoes_drift: uma linha por medição, com o PSI de cada feature em jsonb.</summary>
public sealed class MedicaoDriftConfiguration : IEntityTypeConfiguration<MedicaoDrift>
{
    public void Configure(EntityTypeBuilder<MedicaoDrift> builder)
    {
        builder.ToTable("medicoes_drift");
        builder.HasKey(medicao => medicao.Id);
        builder.Property(medicao => medicao.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(medicao => medicao.VersaoModelo).HasColumnName("versao_modelo");
        builder.Property(medicao => medicao.MedidaEm).HasColumnName("medida_em");
        builder.Property(medicao => medicao.InicioJanela).HasColumnName("inicio_janela");
        builder.Property(medicao => medicao.Amostra).HasColumnName("amostra");
        builder.Property(medicao => medicao.AmostraScore).HasColumnName("amostra_score");
        builder.Property(medicao => medicao.Situacao).HasColumnName("situacao").HasConversion<string>().HasMaxLength(30);
        builder.Property(medicao => medicao.Nivel).HasColumnName("nivel").HasConversion<string>().HasMaxLength(20);
        builder.Property(medicao => medicao.PsiMaximo).HasColumnName("psi_maximo");
        builder.Ignore(medicao => medicao.Psis);
        builder.Property<List<PsiFeature>>("_psis")
            .HasColumnName("psis")
            .HasColumnType("jsonb")
            .HasConversion(ConversoesJson.Psis, ConversoesJson.ComparadorPsis);

        // A visão geral lê a medição mais recente da versão ativa; a retenção apaga pelas mais antigas.
        builder.HasIndex(medicao => new { medicao.VersaoModelo, medicao.MedidaEm });
        builder.HasIndex(medicao => medicao.MedidaEm);
    }
}
