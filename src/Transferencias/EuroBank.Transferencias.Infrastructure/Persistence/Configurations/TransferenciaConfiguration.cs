using EuroBank.Shared.ValueObjects;
using EuroBank.Transferencias.Domain.Entities;
using EuroBank.Transferencias.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EuroBank.Transferencias.Infrastructure.Persistence.Configurations;

/// <summary>Tabela transferencias: o estado da saga de cada transferência.</summary>
public sealed class TransferenciaConfiguration : IEntityTypeConfiguration<Transferencia>
{
    public void Configure(EntityTypeBuilder<Transferencia> builder)
    {
        builder.ToTable("transferencias");

        builder.HasKey(transferencia => transferencia.Id);
        builder.Property(transferencia => transferencia.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Valor, valor => new TransferenciaId(valor))
            .ValueGeneratedNever();

        builder.Property(transferencia => transferencia.Tipo).HasColumnName("tipo").HasConversion<string>().HasMaxLength(20);
        builder.Property(transferencia => transferencia.ContaOrigemId).HasColumnName("conta_origem_id");
        builder.Property(transferencia => transferencia.IbanDestino)
            .HasColumnName("iban_destino")
            .HasConversion(iban => iban.Valor, valor => IbanDestino.Criar(valor))
            .HasMaxLength(34)
            .IsRequired();
        builder.Property(transferencia => transferencia.Bic).HasColumnName("bic").HasMaxLength(11);
        builder.Property(transferencia => transferencia.NomeBeneficiario).HasColumnName("nome_beneficiario").HasMaxLength(140);
        builder.Property(transferencia => transferencia.Valor)
            .HasColumnName("valor")
            .HasConversion(dinheiro => dinheiro.Valor, valor => Dinheiro.De(valor))
            .HasPrecision(18, 2);
        builder.Property(transferencia => transferencia.Tarifa)
            .HasColumnName("tarifa")
            .HasConversion(dinheiro => dinheiro.Valor, valor => Dinheiro.De(valor))
            .HasPrecision(18, 2);
        builder.Property(transferencia => transferencia.Descricao).HasColumnName("descricao").HasMaxLength(140);
        builder.Property(transferencia => transferencia.EmailNotificacao).HasColumnName("email_notificacao").HasMaxLength(254);

        builder.Property(transferencia => transferencia.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(100);
        builder.HasIndex(transferencia => transferencia.IdempotencyKey).IsUnique();
        builder.Property(transferencia => transferencia.HashDoPedido).HasColumnName("hash_do_pedido").HasMaxLength(64);

        builder.Property(transferencia => transferencia.SolicitadaEm).HasColumnName("solicitada_em");
        builder.Property(transferencia => transferencia.ExecutarEm).HasColumnName("executar_em");
        builder.Property(transferencia => transferencia.ExecutarEmEfetivo).HasColumnName("executar_em_efetivo");
        builder.Property(transferencia => transferencia.Estado).HasColumnName("estado").HasConversion<string>().HasMaxLength(30);
        builder.Property(transferencia => transferencia.Motivo).HasColumnName("motivo").HasConversion<string>().HasMaxLength(30);
        builder.Property(transferencia => transferencia.CancelamentoSolicitado).HasColumnName("cancelamento_solicitado");
        builder.Property(transferencia => transferencia.DebitoSemResposta).HasColumnName("debito_sem_resposta");
        builder.Property(transferencia => transferencia.TentativasNoPasso).HasColumnName("tentativas_no_passo");
        builder.Property(transferencia => transferencia.PrazoDoPasso).HasColumnName("prazo_do_passo");
        builder.Property(transferencia => transferencia.AtualizadaEm).HasColumnName("atualizada_em");

        // Concorrência otimista: a API (cancelamento) e o worker (respostas, prazos) alteram a mesma linha.
        builder.Property(transferencia => transferencia.Versao).HasColumnName("versao").IsConcurrencyToken();

        // Soma do limite diário por conta e dia.
        builder.HasIndex(transferencia => new { transferencia.ContaOrigemId, transferencia.ExecutarEmEfetivo });

        builder.Ignore(transferencia => transferencia.EventosDominio);

        builder.HasMany(transferencia => transferencia.Historico)
            .WithOne()
            .HasForeignKey("TransferenciaId")
            .IsRequired();
        builder.Navigation(transferencia => transferencia.Historico)
            .HasField("_historico")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .AutoInclude();
    }
}
