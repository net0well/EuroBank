using EuroBank.Contas.Domain.Entities;
using EuroBank.Contas.Domain.ValueObjects;
using EuroBank.Shared.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EuroBank.Contas.Infrastructure.Persistence.Configurations;

/// <summary>Tabela contas. É a tabela capturada pelo conector de CDC bruto (eurobank.cdc.public.contas).</summary>
public sealed class ContaConfiguration : IEntityTypeConfiguration<Conta>
{
    public void Configure(EntityTypeBuilder<Conta> builder)
    {
        builder.ToTable("contas");

        builder.HasKey(conta => conta.Id);
        builder.Property(conta => conta.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Valor, valor => new ContaId(valor))
            .ValueGeneratedNever();

        builder.Property(conta => conta.Iban)
            .HasColumnName("iban")
            .HasConversion(iban => iban.Valor, valor => Iban.Criar(valor))
            .HasMaxLength(34)
            .IsRequired();
        builder.HasIndex(conta => conta.Iban).IsUnique();

        builder.Property(conta => conta.Titular)
            .HasColumnName("titular")
            .HasConversion(titular => titular.Nome, valor => Titular.Criar(valor))
            .HasMaxLength(Titular.TamanhoMaximo)
            .IsRequired();

        builder.Property(conta => conta.Moeda)
            .HasColumnName("moeda")
            .HasConversion<string>()
            .HasMaxLength(3);

        // O saldo vira só o número; a moeda é a da conta (um conversor não enxerga outra coluna).
        builder.Ignore(conta => conta.Saldo);
        builder.Property<decimal>("_saldo")
            .HasColumnName("saldo")
            .HasPrecision(18, 2);

        builder.Property(conta => conta.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20);

        // Concorrência otimista: o UPDATE leva "WHERE versao = <versão lida>".
        builder.Property(conta => conta.Versao)
            .HasColumnName("versao")
            .IsConcurrencyToken();

        builder.Property(conta => conta.AbertaEm).HasColumnName("aberta_em");

        builder.Ignore(conta => conta.EventosDominio);
        builder.Ignore(conta => conta.TemTransferenciaEmAndamento);

        // As transferências da saga fazem parte do agregado: são carregadas sempre junto com a conta.
        builder.HasMany(conta => conta.Transferencias)
            .WithOne()
            .HasForeignKey("ContaId")
            .IsRequired();
        builder.Navigation(conta => conta.Transferencias)
            .HasField("_transferencias")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .AutoInclude();
    }
}
