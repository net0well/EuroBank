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

        builder.Property(conta => conta.Saldo)
            .HasColumnName("saldo")
            .HasConversion(dinheiro => dinheiro.Valor, valor => Dinheiro.De(valor))
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
    }
}
