using EuroBank.Antifraude.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EuroBank.Antifraude.Infrastructure.Persistence.Configurations;

public sealed class PaisRiscoConfiguration : IEntityTypeConfiguration<PaisRisco>
{
    public void Configure(EntityTypeBuilder<PaisRisco> builder)
    {
        builder.ToTable("paises_risco");

        builder.HasKey(pais => pais.Codigo);
        builder.Property(pais => pais.Codigo).HasColumnName("codigo").HasMaxLength(2);
        builder.Property(pais => pais.IncluidoEm).HasColumnName("incluido_em");
    }
}
