using EuroBank.Simuladores.Comum;
using Npgsql;

namespace EuroBank.Simuladores.BancoExterno.Persistencia;

/// <summary>Sessão do banco externo: inbox e agenda (da base) mais a tabela de liquidações.</summary>
public sealed class SessaoBancoExterno(NpgsqlConnection conexao, NpgsqlTransaction transacao)
    : SessaoSimulador(conexao, transacao)
{
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
}
