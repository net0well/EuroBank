using EuroBank.Simuladores.Comum;
using Npgsql;

namespace EuroBank.Simuladores.BancoExterno.Persistencia;

/// <summary>Database eurobank_banco_externo: as liquidações, além da inbox e da agenda de respostas comuns.</summary>
public sealed class RepositorioBancoExterno(string connectionString) : RepositorioSimulador(connectionString)
{
    protected override string EstruturaPropria => """
        CREATE TABLE IF NOT EXISTS liquidacoes (
            transferencia_id uuid PRIMARY KEY,
            iban_destino text NOT NULL,
            valor numeric(18,2) NOT NULL,
            moeda text NOT NULL,
            status text NOT NULL,
            silenciosa boolean NOT NULL,
            recebida_em timestamptz NOT NULL);
        """;

    public async Task<SessaoBancoExterno> IniciarSessaoAsync(CancellationToken cancellationToken)
    {
        var (conexao, transacao) = await AbrirTransacaoAsync(cancellationToken);
        return new SessaoBancoExterno(conexao, transacao);
    }

    public async Task<string?> ObterStatusAsync(Guid transferenciaId, CancellationToken cancellationToken)
    {
        await using var conexao = await AbrirAsync(cancellationToken);
        await using var comando = new NpgsqlCommand("SELECT status FROM liquidacoes WHERE transferencia_id = @id", conexao);
        comando.Parameters.AddWithValue("id", transferenciaId);
        return (string?)await comando.ExecuteScalarAsync(cancellationToken);
    }
}
