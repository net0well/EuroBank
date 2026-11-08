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
        builder.HasKey(transferencia => transferencia.Id);
        builder.Property(transferencia => transferencia.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property<ContaId>("ContaId")
            .HasColumnName("conta_id")
            .HasConversion(id => id.Valor, valor => new ContaId(valor));
        builder.Property(transferencia => transferencia.Valor)
            .HasColumnName("valor")
            .HasConversion(dinheiro => dinheiro.Valor, valor => Dinheiro.De(valor))
            .HasPrecision(18, 2);
        builder.Property(transferencia => transferencia.Tarifa)
            .HasColumnName("tarifa")
            .HasConversion(dinheiro => dinheiro.Valor, valor => Dinheiro.De(valor))
            .HasPrecision(18, 2);
        builder.Property(transferencia => transferencia.NomeBeneficiario).HasColumnName("nome_beneficiario").HasMaxLength(140);
        builder.Property(transferencia => transferencia.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        builder.Property(transferencia => transferencia.DebitadaEm).HasColumnName("debitada_em");
        builder.Property(transferencia => transferencia.EstornadaEm).HasColumnName("estornada_em");
    }
}
