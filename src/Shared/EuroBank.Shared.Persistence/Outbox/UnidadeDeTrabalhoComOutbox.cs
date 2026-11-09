using System.Diagnostics;
using EuroBank.Shared.Domain;
using EuroBank.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace EuroBank.Shared.Persistence.Outbox;

/// <summary>
/// Grava, numa única transação, as alterações dos agregados e as linhas da outbox com os eventos deles.
/// Ou os dois existem, ou nenhum existe: é isso que torna o outbox confiável.
/// </summary>
/// <remarks>
/// As linhas da outbox são inseridas uma a uma, na ordem em que os eventos aconteceram, e não pelo
/// SaveChanges: o EF Core reordena os INSERTs de um mesmo SaveChanges (pela chave), e o Debezium publica na
/// ordem do WAL. Sem isso, um DepositoRealizado poderia chegar ao Kafka antes do ContaAberta da mesma conta.
/// O DbContext precisa mapear <see cref="MensagemOutbox"/> com <see cref="MensagemOutboxConfiguration"/>.
/// </remarks>
public sealed class UnidadeDeTrabalhoComOutbox<TContexto>(
    TContexto contexto,
    IMapeadorMensagensOutbox mapeador,
    IFilaOutbox fila,
    IOptions<OpcoesOutbox> opcoes) : IUnidadeDeTrabalho
    where TContexto : DbContext
{
    public async Task SalvarAsync(CancellationToken cancellationToken)
    {
        // Primeiro os eventos dos agregados, depois as mensagens avulsas (respostas de saga, comandos),
        // na ordem em que foram enfileiradas. A fila é esvaziada mesmo se a gravação falhar, para um retry
        // não duplicar mensagens.
        var mensagens = ColetarMensagens();
        mensagens.AddRange(fila.Retirar());

        try
        {
            // Dentro de uma transação já aberta (IExecutorTransacional), só participa: quem abriu confirma.
            var transacaoExterna = contexto.Database.CurrentTransaction is not null;
            await using var transacao = transacaoExterna ? null : await contexto.Database.BeginTransactionAsync(cancellationToken);

            await contexto.SaveChangesAsync(cancellationToken);
            foreach (var mensagem in mensagens)
            {
                await InserirNaOutboxAsync(mensagem, cancellationToken);
            }

            if (opcoes.Value.ApagarAposGravar && mensagens.Count > 0)
            {
                // O INSERT já está no WAL: o Debezium publica mesmo com a linha apagada (o DELETE é ignorado).
                var ids = mensagens.Select(mensagem => mensagem.Id).ToArray();
                await contexto.Database.ExecuteSqlAsync($"DELETE FROM outbox WHERE id = ANY({ids})", cancellationToken);
            }

            if (transacao is not null)
            {
                await transacao.CommitAsync(cancellationToken);
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ExcecaoConcorrencia();
        }
        catch (DbUpdateException erro) when (erro.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Duas operações gravaram a mesma chave única ao mesmo tempo (ex.: a mesma Idempotency-Key): quem
            // perdeu recebe 409 e, ao repetir, encontra o que o outro gravou.
            throw new ExcecaoConcorrencia();
        }
    }

    /// <summary>Transforma os eventos de domínio dos agregados rastreados em mensagens, na ordem em que ocorreram.</summary>
    private List<MensagemOutbox> ColetarMensagens()
    {
        // Activity.Current é a requisição HTTP em andamento; o Id no formato W3C é o traceparent.
        var traceparent = Activity.Current?.Id;
        var mensagens = new List<MensagemOutbox>();

        foreach (var agregado in contexto.ChangeTracker.Entries<IRaizDeAgregado>().Select(entrada => entrada.Entity))
        {
            mensagens.AddRange(agregado.EventosDominio.Select(evento => mapeador.Mapear(evento, traceparent)));
            agregado.LimparEventos();
        }

        return mensagens;
    }

    private Task<int> InserirNaOutboxAsync(MensagemOutbox mensagem, CancellationToken cancellationToken) =>
        contexto.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO outbox (id, tipo_agregado, id_agregado, tipo_evento, payload, ocorrido_em, traceparent)
            VALUES ({mensagem.Id}, {mensagem.TipoAgregado}, {mensagem.IdAgregado}, {mensagem.TipoEvento},
                    CAST({mensagem.Payload} AS jsonb), {mensagem.OcorridoEm}, {mensagem.Traceparent})
            """,
            cancellationToken);
}
