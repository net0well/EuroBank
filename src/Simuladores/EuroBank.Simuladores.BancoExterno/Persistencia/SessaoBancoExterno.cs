using Npgsql;

namespace EuroBank.Simuladores.BancoExterno.Persistencia;

/// <summary>Uma transação: registrar a mensagem na inbox, gravar a liquidação e agendar a resposta juntos.</summary>
public sealed class SessaoBancoExterno(NpgsqlConnection conexao, NpgsqlTransaction transacao) : IAsyncDisposable
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

    /// <summary>Grava a liquidação na primeira vez; reenvios do mesmo comando mantêm o registro original.</summary>
    public async Task RegistrarLiquidacaoAsync(
        Guid transferenciaId,
        string ibanDestino,
        decimal valor,
        string moeda,
        string status,
        bool silenciosa,
        DateTimeOffset agora,
        CancellationToken cancellationToken)
    {
        await using var comando = Comando(
            """
            INSERT INTO liquidacoes (transferencia_id, iban_destino, valor, moeda, status, silenciosa, recebida_em)
            VALUES (@id, @iban, @valor, @moeda, @status, @silenciosa, @agora)
            ON CONFLICT (transferencia_id) DO NOTHING
            """);
        comando.Parameters.AddWithValue("id", transferenciaId);
        comando.Parameters.AddWithValue("iban", ibanDestino);
        comando.Parameters.AddWithValue("valor", valor);
        comando.Parameters.AddWithValue("moeda", moeda);
        comando.Parameters.AddWithValue("status", status);
        comando.Parameters.AddWithValue("silenciosa", silenciosa);
        comando.Parameters.AddWithValue("agora", agora);
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<(string Status, bool Silenciosa)?> ObterLiquidacaoAsync(Guid transferenciaId, CancellationToken cancellationToken)
    {
        await using var comando = Comando("SELECT status, silenciosa FROM liquidacoes WHERE transferencia_id = @id");
        comando.Parameters.AddWithValue("id", transferenciaId);
        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);
        return await leitor.ReadAsync(cancellationToken) ? (leitor.GetString(0), leitor.GetBoolean(1)) : null;
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
    }

    private NpgsqlCommand Comando(string sql) => new(sql, conexao, transacao);
}
