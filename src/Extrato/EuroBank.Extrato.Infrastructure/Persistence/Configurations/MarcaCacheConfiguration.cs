using EuroBank.Extrato.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EuroBank.Extrato.Infrastructure.Persistence.Configurations;

/// <summary>Tabela cache_estado, com a linha única já criada pela migration.</summary>
public sealed class MarcaCacheConfiguration : IEntityTypeConfiguration<MarcaCache>
{
    public void Configure(EntityTypeBuilder<MarcaCache> builder)
    {
        builder.ToTable("cache_estado");
        builder.HasKey(marca => marca.Id);
        builder.Property(marca => marca.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(marca => marca.SuspeitoDesde).HasColumnName("suspeito_desde");
        builder.HasData(new MarcaCache { Id = MarcaCache.IdUnico });
    }
}
