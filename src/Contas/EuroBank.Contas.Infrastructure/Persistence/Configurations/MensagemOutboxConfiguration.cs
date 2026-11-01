using EuroBank.Contas.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EuroBank.Contas.Infrastructure.Persistence.Configurations;

/// <summary>
/// Tabela outbox. Os nomes das colunas são referenciados na configuração do Outbox Event Router do Debezium
/// (infra/connect/conectores/eurobank-contas-outbox.json).
/// </summary>
public sealed class MensagemOutboxConfiguration : IEntityTypeConfiguration<MensagemOutbox>
{
    public void Configure(EntityTypeBuilder<MensagemOutbox> builder)
    {
        builder.ToTable("outbox");

        builder.HasKey(mensagem => mensagem.Id);
        builder.Property(mensagem => mensagem.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(mensagem => mensagem.TipoAgregado).HasColumnName("tipo_agregado").HasMaxLength(50);
        builder.Property(mensagem => mensagem.IdAgregado).HasColumnName("id_agregado").HasMaxLength(100);
        builder.Property(mensagem => mensagem.TipoEvento).HasColumnName("tipo_evento").HasMaxLength(100);
        builder.Property(mensagem => mensagem.Payload).HasColumnName("payload").HasColumnType("jsonb");
        builder.Property(mensagem => mensagem.OcorridoEm).HasColumnName("ocorrido_em");
        builder.Property(mensagem => mensagem.Traceparent).HasColumnName("traceparent").HasMaxLength(55);
    }
}
