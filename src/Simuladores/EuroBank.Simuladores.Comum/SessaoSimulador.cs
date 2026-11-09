using Npgsql;

namespace EuroBank.Simuladores.Comum;

/// <summary>
/// Uma transação do simulador: registrar a mensagem na inbox, gravar o que o simulador decidiu e agendar a
/// resposta, tudo junto. Cada simulador herda e acrescenta as próprias tabelas.
/// </summary>
public class SessaoSimulador(NpgsqlConnection conexao, NpgsqlTransaction transacao) : IAsyncDisposable
{
    /// <summary>Registra a mensagem na inbox. Devolve false se ela já tinha sido processada (reentrega do Kafka).</summary>
    public async Task<bool> RegistrarMensagemAsync(string idMensagem, DateTimeOffset agora, CancellationToken cancellationToken)
    {
        await using var comando = Comando(
            "INSERT INTO mensagens_processadas (id, processada_em) VALUES (@id, @agora) ON CONFLICT (id) DO NOTHING");
        comando.Parameters.AddWithValue("id", idMensagem);
        comando.Parameters.AddWithValue("agora", agora);
        return await comando.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task AgendarRespostaAsync(
        Guid transferenciaId,
        string tipo,
        string payload,
        string? traceparent,
        DateTimeOffset responderEm,
        CancellationToken cancellationToken)
    {
        await using var comando = Comando(
            """
            INSERT INTO respostas_agendadas (id, transferencia_id, tipo, payload, traceparent, responder_em)
            VALUES (@id, @transferencia, @tipo, CAST(@payload AS jsonb), @traceparent, @responderEm)
            """);
        comando.Parameters.AddWithValue("id", Guid.CreateVersion7());
        comando.Parameters.AddWithValue("transferencia", transferenciaId);
        comando.Parameters.AddWithValue("tipo", tipo);
        comando.Parameters.AddWithValue("payload", payload);
        comando.Parameters.AddWithValue("traceparent", (object?)traceparent ?? DBNull.Value);
        comando.Parameters.AddWithValue("responderEm", responderEm);
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    public Task ConfirmarAsync(CancellationToken cancellationToken) => transacao.CommitAsync(cancellationToken);

    public async ValueTask DisposeAsync()
    {
        await transacao.DisposeAsync();
        await conexao.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    /// <summary>Comando na conexão e na transação da sessão, para as tabelas próprias de cada simulador.</summary>
    protected NpgsqlCommand Comando(string sql) => new(sql, conexao, transacao);
}
