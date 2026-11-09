using EuroBank.Simuladores.Comum;
using Npgsql;

namespace EuroBank.Simuladores.BancoExterno.Persistencia;

/// <summary>Acesso ao database eurobank_banco_externo com Npgsql e SQL direto (sem EF, de propósito).</summary>
public sealed class RepositorioBancoExterno(string connectionString)
{
    private const string Estrutura = """
        CREATE TABLE IF NOT EXISTS liquidacoes (
            transferencia_id uuid PRIMARY KEY,
            iban_destino text NOT NULL,
            valor numeric(18,2) NOT NULL,
            moeda text NOT NULL,
            status text NOT NULL,
            silenciosa boolean NOT NULL,
            recebida_em timestamptz NOT NULL);

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

    public async Task CriarEstruturaAsync(CancellationToken cancellationToken)
    {
        await BancoDeDados.GarantirDatabaseAsync(connectionString, cancellationToken);
        await using var conexao = await AbrirAsync(cancellationToken);
        await using var comando = new NpgsqlCommand(Estrutura, conexao);
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<SessaoBancoExterno> IniciarSessaoAsync(CancellationToken cancellationToken)
    {
        var conexao = await AbrirAsync(cancellationToken);
        var transacao = await conexao.BeginTransactionAsync(cancellationToken);
        return new SessaoBancoExterno(conexao, transacao);
    }

    public async Task<string?> ObterStatusAsync(Guid transferenciaId, CancellationToken cancellationToken)
    {
        await using var conexao = await AbrirAsync(cancellationToken);
        await using var comando = new NpgsqlCommand("SELECT status FROM liquidacoes WHERE transferencia_id = @id", conexao);
        comando.Parameters.AddWithValue("id", transferenciaId);
        return (string?)await comando.ExecuteScalarAsync(cancellationToken);
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

    private async Task<NpgsqlConnection> AbrirAsync(CancellationToken cancellationToken)
    {
        var conexao = new NpgsqlConnection(connectionString);
        await conexao.OpenAsync(cancellationToken);
        return conexao;
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
