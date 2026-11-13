using EuroBank.Transferencias.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EuroBank.Transferencias.Infrastructure.Persistence.Configurations;

public sealed class ContaConhecidaConfiguration : IEntityTypeConfiguration<ContaConhecida>
{
    public void Configure(EntityTypeBuilder<ContaConhecida> builder)
    {
        builder.ToTable("contas_conhecidas");
        builder.HasKey(conta => conta.Id);
        builder.Property(conta => conta.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(conta => conta.Iban).HasColumnName("iban").HasMaxLength(34);
        builder.HasIndex(conta => conta.Iban).IsUnique();
        builder.Property(conta => conta.Titular).HasColumnName("titular").HasMaxLength(140);
        builder.Property(conta => conta.Moeda).HasColumnName("moeda").HasConversion<string>().HasMaxLength(3);
        builder.Property(conta => conta.AbertaEm).HasColumnName("aberta_em");
        builder.Property(conta => conta.EncerradaEm).HasColumnName("encerrada_em");
    }
}
