using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.ValueObjects;
using EuroBank.Antifraude.Infrastructure.Persistence.Conversions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EuroBank.Antifraude.Infrastructure.Persistence.Configurations;

/// <summary>
/// Tabela versoes_modelo: o arquivo do ML.NET em bytea, métricas, dados e distribuição em jsonb. Os índices únicos
/// parciais garantem no banco uma versão ativa e no máximo uma em sombra.
/// </summary>
public sealed class VersaoModeloConfiguration : IEntityTypeConfiguration<VersaoModelo>
{
    public void Configure(EntityTypeBuilder<VersaoModelo> builder)
    {
        builder.ToTable("versoes_modelo");
        builder.HasKey(versao => versao.Versao);
        builder.Property(versao => versao.Versao).HasColumnName("versao").UseIdentityByDefaultColumn();
        builder.Property(versao => versao.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        builder.Property(versao => versao.Arquivo).HasColumnName("arquivo").HasColumnType("bytea");
        builder.Property(versao => versao.Metricas).HasColumnName("metricas").HasColumnType("jsonb").HasConversion(ConversoesJson.Metricas);
        builder.Property(versao => versao.LimiarRecomendado).HasColumnName("limiar_recomendado");
        builder.Property(versao => versao.Dados).HasColumnName("dados_treino").HasColumnType("jsonb").HasConversion(ConversoesJson.DadosTreino);
        builder.Ignore(versao => versao.Distribuicao);
        builder.Property<List<DistribuicaoFeature>>("_distribuicao")
            .HasColumnName("distribuicao")
            .HasColumnType("jsonb")
            .HasConversion(ConversoesJson.Distribuicao, ConversoesJson.ComparadorDistribuicao);
        builder.Property(versao => versao.TreinadaEm).HasColumnName("treinada_em");
        builder.Property(versao => versao.AtivadaEm).HasColumnName("ativada_em");
        builder.Property(versao => versao.AposentadaEm).HasColumnName("aposentada_em");

        builder.HasIndex(versao => versao.Status, "ux_versoes_modelo_ativa")
            .IsUnique()
            .HasFilter($"status = '{StatusModelo.Ativa}'");
        builder.HasIndex(versao => versao.Status, "ux_versoes_modelo_challenger")
            .IsUnique()
            .HasFilter($"status = '{StatusModelo.Challenger}'");
    }
}
