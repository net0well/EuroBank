using EuroBank.Simuladores.Cambio.TransferObjects;
using EuroBank.Simuladores.Comum;
using Npgsql;

namespace EuroBank.Simuladores.Cambio.Persistencia;

/// <summary>Database eurobank_cambio: as cotações emitidas, além da inbox e da agenda de respostas comuns.</summary>
public sealed class RepositorioCambio(string connectionString) : RepositorioSimulador(connectionString)
{
    protected override string EstruturaPropria => """
        CREATE TABLE IF NOT EXISTS cotacoes (
            id uuid PRIMARY KEY,
            transferencia_id uuid NOT NULL,
            moeda_origem text NOT NULL,
            moeda_destino text NOT NULL,
            taxa numeric(18,6) NOT NULL,
            valor_origem numeric(18,2) NOT NULL,
            valor_destino numeric(18,2) NOT NULL,
            emitida_em timestamptz NOT NULL,
            valida_ate timestamptz NOT NULL);

        CREATE INDEX IF NOT EXISTS ix_cotacoes_transferencia ON cotacoes (transferencia_id);
        """;

    public async Task<SessaoCambio> IniciarSessaoAsync(CancellationToken cancellationToken)
    {
        var (conexao, transacao) = await AbrirTransacaoAsync(cancellationToken);
        return new SessaoCambio(conexao, transacao);
    }

    /// <summary>A cotação mais recente da transferência ainda válida em <paramref name="agora"/>, ou nula.</summary>
    public async Task<CotacaoEmitida?> ObterValidaAsync(Guid transferenciaId, DateTimeOffset agora, CancellationToken cancellationToken)
    {
        await using var conexao = await AbrirAsync(cancellationToken);
        await using var comando = new NpgsqlCommand(
            """
            SELECT id, moeda_origem, moeda_destino, taxa, valor_origem, valor_destino, valida_ate FROM cotacoes
            WHERE transferencia_id = @id AND valida_ate > @agora
            ORDER BY emitida_em DESC, id DESC
            LIMIT 1
            """,
            conexao);
        comando.Parameters.AddWithValue("id", transferenciaId);
        comando.Parameters.AddWithValue("agora", agora);

        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);
        if (!await leitor.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new CotacaoEmitida(
            transferenciaId,
            leitor.GetGuid(0),
            leitor.GetString(1),
            leitor.GetString(2),
            leitor.GetDecimal(3),
            leitor.GetDecimal(4),
            leitor.GetDecimal(5),
            leitor.GetFieldValue<DateTimeOffset>(6));
    }

    /// <summary>Ids das cotações emitidas para a transferência, da mais antiga para a mais nova.</summary>
    public async Task<IReadOnlyList<Guid>> ListarCotacoesAsync(Guid transferenciaId, CancellationToken cancellationToken)
    {
        await using var conexao = await AbrirAsync(cancellationToken);
        await using var comando = new NpgsqlCommand(
            "SELECT id FROM cotacoes WHERE transferencia_id = @id ORDER BY emitida_em, id", conexao);
        comando.Parameters.AddWithValue("id", transferenciaId);

        var ids = new List<Guid>();
        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);
        while (await leitor.ReadAsync(cancellationToken))
        {
            ids.Add(leitor.GetGuid(0));
        }

        return ids;
    }
}
