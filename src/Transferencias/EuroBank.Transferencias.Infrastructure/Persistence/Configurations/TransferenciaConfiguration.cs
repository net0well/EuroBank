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
        builder.Property(transferencia => transferencia.IbanOrigem).HasColumnName("iban_origem").HasMaxLength(34);
        builder.Property(transferencia => transferencia.NomeOrigem).HasColumnName("nome_origem").HasMaxLength(140);
        builder.Property(transferencia => transferencia.MoedaOrigem).HasColumnName("moeda_origem").HasConversion<string>().HasMaxLength(3);
        builder.Property(transferencia => transferencia.ContaDestinoId).HasColumnName("conta_destino_id");
        builder.Property(transferencia => transferencia.IbanDestino)
            .HasColumnName("iban_destino")
            .HasConversion(iban => iban.Valor, valor => IbanDestino.Criar(valor))
            .HasMaxLength(34)
            .IsRequired();
        builder.Property(transferencia => transferencia.Bic).HasColumnName("bic").HasMaxLength(11);
        builder.Property(transferencia => transferencia.NomeBeneficiario).HasColumnName("nome_beneficiario").HasMaxLength(140);
        builder.Property(transferencia => transferencia.MoedaDestino).HasColumnName("moeda_destino").HasConversion<string>().HasMaxLength(3);
        builder.Property(transferencia => transferencia.MoedaDoValor).HasColumnName("moeda_do_valor").HasConversion<string>().HasMaxLength(3);

        // Valores só como números; cada um tem a moeda numa coluna própria (um conversor não enxerga outra coluna).
        builder.Ignore(transferencia => transferencia.Valor);
        builder.Ignore(transferencia => transferencia.Tarifa);
        builder.Ignore(transferencia => transferencia.ValorDebitado);
        builder.Ignore(transferencia => transferencia.ValorDestino);
        builder.Ignore(transferencia => transferencia.Cotacao);
        builder.Ignore(transferencia => transferencia.PrecisaDeCambio);
        builder.Property<decimal>("_valor").HasColumnName("valor").HasPrecision(18, 2);
        builder.Property<decimal>("_tarifa").HasColumnName("tarifa").HasPrecision(18, 2);
        builder.Property<decimal?>("_valorDebitado").HasColumnName("valor_debitado").HasPrecision(18, 2);
        builder.Property<decimal?>("_valorDestino").HasColumnName("valor_destino").HasPrecision(18, 2);
        builder.Property<Guid?>("_cotacaoId").HasColumnName("cotacao_id");
        builder.Property<decimal?>("_cotacaoTaxa").HasColumnName("cotacao_taxa").HasPrecision(18, 6);
        builder.Property<DateTimeOffset?>("_cotacaoValidaAte").HasColumnName("cotacao_valida_ate");
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

        // Varredura do worker: índices parciais, só com as linhas que ela pode precisar.
        builder.HasIndex(transferencia => transferencia.ExecutarEmEfetivo)
            .HasDatabaseName("ix_transferencias_agendadas")
            .HasFilter("estado = 'Agendada'");
        builder.HasIndex(transferencia => transferencia.PrazoDoPasso)
            .HasDatabaseName("ix_transferencias_prazo_do_passo")
            .HasFilter("prazo_do_passo IS NOT NULL");

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
