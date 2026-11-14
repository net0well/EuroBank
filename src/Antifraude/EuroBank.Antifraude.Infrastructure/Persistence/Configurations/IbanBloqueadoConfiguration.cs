using EuroBank.Antifraude.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EuroBank.Antifraude.Infrastructure.Persistence.Configurations;

public sealed class IbanBloqueadoConfiguration : IEntityTypeConfiguration<IbanBloqueado>
{
    public void Configure(EntityTypeBuilder<IbanBloqueado> builder)
    {
        builder.ToTable("ibans_bloqueados");

        builder.HasKey(bloqueado => bloqueado.Iban);
        builder.Property(bloqueado => bloqueado.Iban).HasColumnName("iban").HasMaxLength(34);
        builder.Property(bloqueado => bloqueado.Motivo).HasColumnName("motivo").HasMaxLength(500);
        builder.Property(bloqueado => bloqueado.BloqueadoPor).HasColumnName("bloqueado_por").HasMaxLength(100);
        builder.Property(bloqueado => bloqueado.BloqueadoEm).HasColumnName("bloqueado_em");
    }
}
