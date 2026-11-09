using EuroBank.Extrato.Domain.Entities;
using EuroBank.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EuroBank.Extrato.Infrastructure.Persistence.Configurations;

public sealed class LancamentoConfiguration : IEntityTypeConfiguration<Lancamento>
{
    public void Configure(EntityTypeBuilder<Lancamento> builder)
    {
        builder.ToTable("lancamentos");

        // ValueGeneratedNever: o id nasce no domínio; assim o EF trata um lançamento novo, adicionado à coleção
        // de um extrato já carregado, como INSERT (e não como UPDATE de algo existente).
        builder.HasKey(lancamento => lancamento.Id);
        builder.Property(lancamento => lancamento.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property<Guid>("ContaId").HasColumnName("conta_id");
        builder.Property(lancamento => lancamento.Tipo).HasColumnName("tipo").HasConversion<string>().HasMaxLength(30);
        // Valores só como números, ao lado da moeda da conta (um conversor não enxerga outra coluna).
        builder.Property(lancamento => lancamento.Moeda).HasColumnName("moeda").HasConversion<string>().HasMaxLength(3);
        builder.Ignore(lancamento => lancamento.Valor);
        builder.Ignore(lancamento => lancamento.SaldoApos);
        builder.Ignore(lancamento => lancamento.Tarifa);
        builder.Ignore(lancamento => lancamento.Cambio);
        builder.Property<decimal>("_valor").HasColumnName("valor").HasPrecision(18, 2);
        builder.Property<decimal>("_saldoApos").HasColumnName("saldo_apos").HasPrecision(18, 2);
        builder.Property<decimal?>("_tarifa").HasColumnName("tarifa").HasPrecision(18, 2);
        builder.Property(lancamento => lancamento.Versao).HasColumnName("versao");
        builder.Property(lancamento => lancamento.OcorridoEm).HasColumnName("ocorrido_em");
        builder.Property(lancamento => lancamento.Descricao).HasColumnName("descricao").HasMaxLength(200);
        builder.Property(lancamento => lancamento.TransferenciaId).HasColumnName("transferencia_id");

        // Câmbio: o outro lado da transferência (moeda, valor) e a cotação usada.
        builder.Property<Moeda?>("_moedaCambio").HasColumnName("moeda_cambio").HasConversion<string>().HasMaxLength(3);
        builder.Property<decimal?>("_valorCambio").HasColumnName("valor_cambio").HasPrecision(18, 2);
        builder.Property<decimal?>("_cotacao").HasColumnName("cotacao").HasPrecision(18, 6);

        // Última linha de defesa da idempotência: no máximo um lançamento por versão da conta.
        builder.HasIndex("ContaId", nameof(Lancamento.Versao)).IsUnique();
    }
}
