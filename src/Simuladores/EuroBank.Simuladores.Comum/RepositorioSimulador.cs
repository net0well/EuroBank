using Npgsql;

namespace EuroBank.Simuladores.Comum;

/// <summary>
/// Acesso ao database de um simulador com Npgsql e SQL direto (sem EF, de propósito): a inbox escrita à mão, a
/// agenda de respostas e a entrega das vencidas. Cada simulador acrescenta a própria estrutura.
/// </summary>
public abstract class RepositorioSimulador(string connectionString)
{
    private const string EstruturaComum = """
        CREATE TABLE IF NOT EXISTS respostas_agendadas (
            id uuid PRIMARY KEY,
            transferencia_id uuid NOT NULL,
            tipo text NOT NULL,
            payload jsonb NOT NULL,
            traceparent text NULL,
            responder_em timestamptz NOT NULL,
            enviada_em timestamptz NULL);

        CREATE INDEX IF NOT EXISTS ix_respostas_pendentes
            ON respostas_agendadas (responder_em) WHERE enviada_em IS NULL;

        -- Inbox escrita à mão: o id de cada mensagem já processada (o Kafka entrega pelo menos uma vez).
        CREATE TABLE IF NOT EXISTS mensagens_processadas (
            id text PRIMARY KEY,
            processada_em timestamptz NOT NULL);
        """;

    /// <summary>CREATE TABLE IF NOT EXISTS das tabelas próprias do simulador.</summary>
    protected abstract string EstruturaPropria { get; }

    public async Task CriarEstruturaAsync(CancellationToken cancellationToken)
    {
        await BancoDeDados.GarantirDatabaseAsync(connectionString, cancellationToken);
        await using var conexao = await AbrirAsync(cancellationToken);
        await using var comando = new NpgsqlCommand(EstruturaComum + EstruturaPropria, conexao);
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RespostaAgendada>> ListarRespostasAsync(Guid transferenciaId, CancellationToken cancellationToken)
    {
        await using var conexao = await AbrirAsync(cancellationToken);
        await using var comando = new NpgsqlCommand(
            """
            SELECT id, transferencia_id, tipo, payload::text, traceparent, responder_em
            FROM respostas_agendadas WHERE transferencia_id = @id ORDER BY responder_em, id
            """, conexao);
        comando.Parameters.AddWithValue("id", transferenciaId);
        return await LerRespostasAsync(comando, cancellationToken);
    }

    /// <summary>
    /// Entrega as respostas vencidas: trava as linhas (SKIP LOCKED permite várias instâncias), chama a entrega e
    /// marca como enviadas na mesma transação. Se a gravação falhar depois da entrega, a resposta sai de novo:
    /// pelo menos uma vez, como o resto do laboratório.
    /// </summary>
    public async Task<int> EntregarVencidasAsync(
        DateTimeOffset agora,
        Func<RespostaAgendada, Task> entregar,
        CancellationToken cancellationToken)
    {
        await using var conexao = await AbrirAsync(cancellationToken);
        await using var transacao = await conexao.BeginTransactionAsync(cancellationToken);

        await using var selecionar = new NpgsqlCommand(
            """
            SELECT id, transferencia_id, tipo, payload::text, traceparent, responder_em
            FROM respostas_agendadas
            WHERE enviada_em IS NULL AND responder_em <= @agora
            ORDER BY responder_em
            LIMIT 50
            FOR UPDATE SKIP LOCKED
            """, conexao, transacao);
        selecionar.Parameters.AddWithValue("agora", agora);
        var vencidas = await LerRespostasAsync(selecionar, cancellationToken);

        foreach (var resposta in vencidas)
        {
            await entregar(resposta);
            await using var marcar = new NpgsqlCommand(
                "UPDATE respostas_agendadas SET enviada_em = @agora WHERE id = @id", conexao, transacao);
            marcar.Parameters.AddWithValue("agora", agora);
            marcar.Parameters.AddWithValue("id", resposta.Id);
            await marcar.ExecuteNonQueryAsync(cancellationToken);
        }

        await transacao.CommitAsync(cancellationToken);
        return vencidas.Count;
    }

    protected async Task<NpgsqlConnection> AbrirAsync(CancellationToken cancellationToken)
    {
        var conexao = new NpgsqlConnection(connectionString);
        await conexao.OpenAsync(cancellationToken);
        return conexao;
    }

    /// <summary>Conexão aberta com uma transação iniciada, para montar a sessão do simulador.</summary>
    protected async Task<(NpgsqlConnection Conexao, NpgsqlTransaction Transacao)> AbrirTransacaoAsync(
        CancellationToken cancellationToken)
    {
        var conexao = await AbrirAsync(cancellationToken);
        return (conexao, await conexao.BeginTransactionAsync(cancellationToken));
    }

    private static async Task<IReadOnlyList<RespostaAgendada>> LerRespostasAsync(
        NpgsqlCommand comando,
        CancellationToken cancellationToken)
    {
        var respostas = new List<RespostaAgendada>();
        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);
        while (await leitor.ReadAsync(cancellationToken))
        {
            respostas.Add(new RespostaAgendada(
                leitor.GetGuid(0),
                leitor.GetGuid(1),
                leitor.GetString(2),
                leitor.GetString(3),
                await leitor.IsDBNullAsync(4, cancellationToken) ? null : leitor.GetString(4),
                leitor.GetFieldValue<DateTimeOffset>(5)));
        }

        return respostas;
    }
}
