using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EuroBank.Antifraude.Infrastructure.Persistence.Configurations;

/// <summary>Tabela casos_revisao, com a auditoria em casos_revisao_auditoria.</summary>
public sealed class CasoRevisaoConfiguration : IEntityTypeConfiguration<CasoRevisao>
{
    public void Configure(EntityTypeBuilder<CasoRevisao> builder)
    {
        builder.ToTable("casos_revisao");

        builder.HasKey(caso => caso.Id);
        builder.Property(caso => caso.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Valor, valor => new CasoId(valor))
            .ValueGeneratedNever();

        builder.Property(caso => caso.AnaliseId)
            .HasColumnName("analise_id")
            .HasConversion(id => id.Valor, valor => new AnaliseId(valor));
        builder.HasIndex(caso => caso.AnaliseId).IsUnique();
        builder.Property(caso => caso.OperacaoId).HasColumnName("operacao_id");
        builder.HasIndex(caso => caso.OperacaoId).IsUnique();
        builder.Property(caso => caso.ContaId).HasColumnName("conta_id");
        builder.Property(caso => caso.ScoreFinal).HasColumnName("score_final");
        builder.Property(caso => caso.Estado).HasColumnName("estado").HasConversion<string>().HasMaxLength(30);
        builder.Property(caso => caso.Analista).HasColumnName("analista").HasMaxLength(100);
        builder.Property(caso => caso.AbertoEm).HasColumnName("aberto_em");
        builder.Property(caso => caso.ExpiraEm).HasColumnName("expira_em");
        builder.Property(caso => caso.AssumidoEm).HasColumnName("assumido_em");
        builder.Property(caso => caso.DecididoEm).HasColumnName("decidido_em");
        builder.Property(caso => caso.Justificativa).HasColumnName("justificativa").HasMaxLength(1000);
        builder.Property(caso => caso.BloquearIban).HasColumnName("bloquear_iban");

        // Decisão do analista e expiração no mesmo caso: vence quem gravar primeiro.
        builder.Property(caso => caso.Versao).HasColumnName("versao").IsConcurrencyToken();

        // Fila de revisão e expiração: só os casos pendentes.
        builder.HasIndex(caso => caso.ExpiraEm)
            .HasDatabaseName("ix_casos_revisao_pendentes")
            .HasFilter("estado IN ('Aberto', 'EmAnalise')");

        builder.Ignore(caso => caso.EstaPendente);
        builder.Ignore(caso => caso.EventosDominio);

        builder.HasMany(caso => caso.Auditoria)
            .WithOne()
            .HasForeignKey("CasoId")
            .IsRequired();
        builder.Navigation(caso => caso.Auditoria)
            .HasField("_auditoria")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .AutoInclude();
    }
}
