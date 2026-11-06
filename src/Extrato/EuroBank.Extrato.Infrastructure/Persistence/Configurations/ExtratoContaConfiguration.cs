using EuroBank.Extrato.Domain.Entities;
using EuroBank.Shared.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EuroBank.Extrato.Infrastructure.Persistence.Configurations;

public sealed class ExtratoContaConfiguration : IEntityTypeConfiguration<ExtratoConta>
{
    public void Configure(EntityTypeBuilder<ExtratoConta> builder)
    {
        builder.ToTable("extratos");

        builder.HasKey(extrato => extrato.ContaId);
        builder.Property(extrato => extrato.ContaId).HasColumnName("conta_id").ValueGeneratedNever();
        builder.Property(extrato => extrato.Iban).HasColumnName("iban").HasMaxLength(34);
        builder.Property(extrato => extrato.Titular).HasColumnName("titular").HasMaxLength(120);
        builder.Property(extrato => extrato.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        builder.Property(extrato => extrato.SaldoAtual)
            .HasColumnName("saldo_atual")
            .HasConversion(dinheiro => dinheiro.Valor, valor => Dinheiro.De(valor))
            .HasPrecision(18, 2);

        // Duas instâncias aplicando eventos da mesma conta (ex.: durante um rebalance) não sobrescrevem uma à
        // outra: a segunda recebe conflito, tenta de novo e a regra de versão descarta o evento repetido.
        builder.Property(extrato => extrato.UltimaVersao).HasColumnName("ultima_versao").IsConcurrencyToken();
        builder.Property(extrato => extrato.AtualizadoEm).HasColumnName("atualizado_em");

        builder.HasMany(extrato => extrato.Lancamentos)
            .WithOne()
            .HasForeignKey("ContaId")
            .IsRequired();
        builder.Navigation(extrato => extrato.Lancamentos)
            .HasField("_lancamentos")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
