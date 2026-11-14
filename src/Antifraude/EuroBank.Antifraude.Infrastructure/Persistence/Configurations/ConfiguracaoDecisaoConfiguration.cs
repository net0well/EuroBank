using EuroBank.Antifraude.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EuroBank.Antifraude.Infrastructure.Persistence.Configurations;

/// <summary>Tabela configuracoes_decisao: uma linha por versão; a atual é a de maior versão.</summary>
public sealed class ConfiguracaoDecisaoConfiguration : IEntityTypeConfiguration<ConfiguracaoDecisao>
{
    public void Configure(EntityTypeBuilder<ConfiguracaoDecisao> builder)
    {
        builder.ToTable("configuracoes_decisao");

        builder.HasKey(configuracao => configuracao.Versao);
        builder.Property(configuracao => configuracao.Versao).HasColumnName("versao").ValueGeneratedNever();
        builder.Property(configuracao => configuracao.LimiarRevisao).HasColumnName("limiar_revisao");
        builder.Property(configuracao => configuracao.LimiarRecusa).HasColumnName("limiar_recusa");
        builder.Property(configuracao => configuracao.PesoModelo).HasColumnName("peso_modelo");
        builder.Property(configuracao => configuracao.CriadaEm).HasColumnName("criada_em");
    }
}
