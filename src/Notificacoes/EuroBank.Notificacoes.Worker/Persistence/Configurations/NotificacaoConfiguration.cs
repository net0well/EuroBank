using EuroBank.Notificacoes.Worker.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EuroBank.Notificacoes.Worker.Persistence.Configurations;

public sealed class NotificacaoConfiguration : IEntityTypeConfiguration<Notificacao>
{
    public void Configure(EntityTypeBuilder<Notificacao> builder)
    {
        builder.ToTable("notificacoes");

        // A chave é o id do evento: um segundo INSERT do mesmo evento falha, e o retry encontra a notificação existente.
        builder.HasKey(notificacao => notificacao.Id);
        builder.Property(notificacao => notificacao.Id).HasColumnName("evento_id").ValueGeneratedNever();
        builder.Property(notificacao => notificacao.TransferenciaId).HasColumnName("transferencia_id");
        builder.Property(notificacao => notificacao.TipoEvento).HasColumnName("tipo_evento").HasMaxLength(60);
        builder.Property(notificacao => notificacao.Destinatario).HasColumnName("destinatario").HasMaxLength(254);
        builder.Property(notificacao => notificacao.Assunto).HasColumnName("assunto");
        builder.Property(notificacao => notificacao.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        builder.Property(notificacao => notificacao.Tentativas).HasColumnName("tentativas");
        builder.Property(notificacao => notificacao.UltimoErro).HasColumnName("ultimo_erro").HasMaxLength(500);
        builder.Property(notificacao => notificacao.CriadaEm).HasColumnName("criada_em");
        builder.Property(notificacao => notificacao.AtualizadaEm).HasColumnName("atualizada_em");
        builder.Property(notificacao => notificacao.EnviadaEm).HasColumnName("enviada_em");

        builder.HasIndex(notificacao => notificacao.TransferenciaId);
    }
}
