using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Infrastructure.Persistence.Conversions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EuroBank.Antifraude.Infrastructure.Persistence.Configurations;

/// <summary>Tabela regras: uma linha por código, com os parâmetros em jsonb.</summary>
public sealed class RegraConfiguration : IEntityTypeConfiguration<Regra>
{
    public void Configure(EntityTypeBuilder<Regra> builder)
    {
        builder.ToTable("regras");

        builder.HasKey(regra => regra.Codigo);
        builder.Property(regra => regra.Codigo).HasColumnName("codigo").HasConversion<string>().HasMaxLength(40);
        builder.Property(regra => regra.Tipo).HasColumnName("tipo").HasConversion<string>().HasMaxLength(10);
        builder.Property(regra => regra.Descricao).HasColumnName("descricao").HasMaxLength(200);
        builder.Property(regra => regra.Peso).HasColumnName("peso");
        builder.Property(regra => regra.Efeito).HasColumnName("efeito").HasConversion<string>().HasMaxLength(10);
        builder.Property(regra => regra.Ativa).HasColumnName("ativa");
        builder.Property(regra => regra.Sombra).HasColumnName("sombra");
        builder.Property(regra => regra.AtualizadaEm).HasColumnName("atualizada_em");

        builder.Ignore(regra => regra.Parametros);
        builder.Property<Dictionary<string, decimal>>("_parametros")
            .HasColumnName("parametros")
            .HasColumnType("jsonb")
            .HasConversion(ConversoesJson.Parametros, ConversoesJson.ComparadorParametros);
    }
}
