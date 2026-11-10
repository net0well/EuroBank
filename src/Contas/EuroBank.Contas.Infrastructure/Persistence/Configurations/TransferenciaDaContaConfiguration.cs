using EuroBank.Contas.Domain.Entities;
using EuroBank.Contas.Domain.ValueObjects;
using EuroBank.Shared.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EuroBank.Contas.Infrastructure.Persistence.Configurations;

/// <summary>Trava semântica e registro de idempotência das transferências pedidas por saga.</summary>
public sealed class TransferenciaDaContaConfiguration : IEntityTypeConfiguration<TransferenciaDaConta>
{
    public void Configure(EntityTypeBuilder<TransferenciaDaConta> builder)
    {
        builder.ToTable("transferencias_conta");

        // O id vem da saga; ValueGeneratedNever faz o EF tratar um registro novo na coleção como INSERT.
        // Conta e transferência: no câmbio interno o mesmo id aparece na origem (débito) e no destino (crédito).
        builder.HasKey("ContaId", nameof(TransferenciaDaConta.Id));
        builder.Property(transferencia => transferencia.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property<ContaId>("ContaId")
            .HasColumnName("conta_id")
            .HasConversion(id => id.Valor, valor => new ContaId(valor));
        builder.Property(transferencia => transferencia.Moeda).HasColumnName("moeda").HasConversion<string>().HasMaxLength(3);
        builder.Ignore(transferencia => transferencia.Valor);
        builder.Ignore(transferencia => transferencia.Tarifa);
        builder.Property<decimal>("_valor").HasColumnName("valor").HasPrecision(18, 2);
        builder.Property<decimal>("_tarifa").HasColumnName("tarifa").HasPrecision(18, 2);
        builder.Property(transferencia => transferencia.NomeBeneficiario).HasColumnName("nome_beneficiario").HasMaxLength(140);
        builder.Property(transferencia => transferencia.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        builder.Property(transferencia => transferencia.DebitadaEm).HasColumnName("debitada_em");
        builder.Property(transferencia => transferencia.EstornadaEm).HasColumnName("estornada_em");
    }
}
