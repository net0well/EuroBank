using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.ValueObjects;
using EuroBank.Antifraude.Infrastructure.Persistence.Conversions;
using EuroBank.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EuroBank.Antifraude.Infrastructure.Persistence.Configurations;

/// <summary>Tabela analises: uma por operação (operacao_id único), com os sinais das regras em jsonb.</summary>
public sealed class AnaliseConfiguration : IEntityTypeConfiguration<Analise>
{
    public void Configure(EntityTypeBuilder<Analise> builder)
    {
        builder.ToTable("analises");

        builder.HasKey(analise => analise.Id);
        builder.Property(analise => analise.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Valor, valor => new AnaliseId(valor))
            .ValueGeneratedNever();

        // Avaliar de novo a mesma operação devolve esta análise; duas avaliações simultâneas: uma perde no índice.
        builder.Property(analise => analise.OperacaoId).HasColumnName("operacao_id");
        builder.HasIndex(analise => analise.OperacaoId).IsUnique();
        builder.Property(analise => analise.ContaId).HasColumnName("conta_id");
        builder.HasIndex(analise => analise.ContaId);

        builder.Ignore(analise => analise.Contexto);
        builder.Property<TipoOperacao>("_tipoOperacao").HasColumnName("tipo_operacao").HasConversion<string>().HasMaxLength(30);
        builder.Property<decimal>("_valor").HasColumnName("valor").HasPrecision(18, 2);
        builder.Property<Moeda>("_moeda").HasColumnName("moeda").HasConversion<string>().HasMaxLength(3);
        builder.Property<string?>("_ibanDestino").HasColumnName("iban_destino").HasMaxLength(34);
        builder.Property<DateTimeOffset>("_momento").HasColumnName("momento");

        builder.Ignore(analise => analise.Sinais);
        builder.Property<List<SinalRegra>>("_sinais")
            .HasColumnName("sinais")
            .HasColumnType("jsonb")
            .HasConversion(ConversoesJson.Sinais, ConversoesJson.ComparadorSinais);

        builder.Property(analise => analise.ScoreFinal).HasColumnName("score_final");
        builder.Property(analise => analise.Decisao).HasColumnName("decisao").HasConversion<string>().HasMaxLength(20);
        builder.Property(analise => analise.VersaoConfiguracao).HasColumnName("versao_configuracao");
        builder.Property(analise => analise.CriadaEm).HasColumnName("criada_em");
        builder.Property(analise => analise.DecididaEm).HasColumnName("decidida_em");
        builder.Property(analise => analise.Analista).HasColumnName("analista").HasMaxLength(100);
        builder.Property(analise => analise.Justificativa).HasColumnName("justificativa").HasMaxLength(1000);

        builder.Ignore(analise => analise.EstaEmRevisao);
        builder.Ignore(analise => analise.EventosDominio);
    }
}
