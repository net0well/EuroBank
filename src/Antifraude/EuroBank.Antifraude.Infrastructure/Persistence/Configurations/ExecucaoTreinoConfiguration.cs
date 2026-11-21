using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.ValueObjects;
using EuroBank.Antifraude.Infrastructure.Persistence.Conversions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EuroBank.Antifraude.Infrastructure.Persistence.Configurations;

/// <summary>
/// Tabela execucoes_treino: parâmetros, etapas, log e relatório em jsonb (a execução é lida inteira, e o progresso
/// muda a cada segundo durante o treino). Um índice único parcial, na migration, aceita uma ativa só, e um gatilho
/// avisa o treinador (LISTEN antifraude_treinos) quando uma é solicitada.
/// </summary>
public sealed class ExecucaoTreinoConfiguration : IEntityTypeConfiguration<ExecucaoTreino>
{
    public void Configure(EntityTypeBuilder<ExecucaoTreino> builder)
    {
        builder.ToTable("execucoes_treino");
        builder.HasKey(execucao => execucao.Id);
        builder.Property(execucao => execucao.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(execucao => execucao.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        builder.Property(execucao => execucao.Parametros)
            .HasColumnName("parametros")
            .HasColumnType("jsonb")
            .HasConversion(ConversoesJson.ParametrosTreino);
        builder.Property(execucao => execucao.Solicitante).HasColumnName("solicitante").HasMaxLength(100);
        builder.Ignore(execucao => execucao.Etapas);
        builder.Ignore(execucao => execucao.EtapaAtual);
        builder.Property<List<EtapaExecutada>>("_etapas")
            .HasColumnName("etapas")
            .HasColumnType("jsonb")
            .HasConversion(ConversoesJson.Etapas, ConversoesJson.ComparadorEtapas);
        builder.Property(execucao => execucao.Progresso).HasColumnName("progresso");
        builder.Ignore(execucao => execucao.Registros);
        builder.Property<List<RegistroTreino>>("_registros")
            .HasColumnName("registros")
            .HasColumnType("jsonb")
            .HasConversion(ConversoesJson.Registros, ConversoesJson.ComparadorRegistros);
        builder.Property(execucao => execucao.Relatorio).HasColumnName("relatorio").HasColumnType("jsonb").HasConversion(ConversoesJson.Relatorio);
        builder.Property(execucao => execucao.Versao).HasColumnName("versao");
        builder.Property(execucao => execucao.Erro).HasColumnName("erro");
        builder.Property(execucao => execucao.CancelamentoPedido).HasColumnName("cancelamento_pedido");
        builder.Property(execucao => execucao.SolicitadaEm).HasColumnName("solicitada_em");
        builder.Property(execucao => execucao.IniciadaEm).HasColumnName("iniciada_em");
        builder.Property(execucao => execucao.TerminadaEm).HasColumnName("terminada_em");
        builder.Property(execucao => execucao.AtualizadaEm).HasColumnName("atualizada_em");
        builder.Ignore(execucao => execucao.Ativa);

        builder.HasIndex(execucao => execucao.SolicitadaEm, "ix_execucoes_treino_solicitada_em").IsDescending();
    }
}
