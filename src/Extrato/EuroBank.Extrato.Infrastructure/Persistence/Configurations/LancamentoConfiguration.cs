using EuroBank.Extrato.Domain.Entities;
using EuroBank.Shared.ValueObjects;
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
        builder.Property(lancamento => lancamento.Valor)
            .HasColumnName("valor")
            .HasConversion(dinheiro => dinheiro.Valor, valor => Dinheiro.De(valor))
            .HasPrecision(18, 2);
        builder.Property(lancamento => lancamento.SaldoApos)
            .HasColumnName("saldo_apos")
            .HasConversion(dinheiro => dinheiro.Valor, valor => Dinheiro.De(valor))
            .HasPrecision(18, 2);
        builder.Property(lancamento => lancamento.Versao).HasColumnName("versao");
        builder.Property(lancamento => lancamento.OcorridoEm).HasColumnName("ocorrido_em");
        builder.Property(lancamento => lancamento.Descricao).HasColumnName("descricao").HasMaxLength(200);
        builder.Property(lancamento => lancamento.TransferenciaId).HasColumnName("transferencia_id");

        // Última linha de defesa da idempotência: no máximo um lançamento por versão da conta.
        builder.HasIndex("ContaId", nameof(Lancamento.Versao)).IsUnique();
    }
}
