using EuroBank.Simuladores.Cambio.TransferObjects;
using EuroBank.Simuladores.Comum;
using Npgsql;

namespace EuroBank.Simuladores.Cambio.Persistencia;

/// <summary>Sessão do câmbio: inbox e agenda (da base) mais a tabela de cotações.</summary>
public sealed class SessaoCambio(NpgsqlConnection conexao, NpgsqlTransaction transacao)
    : SessaoSimulador(conexao, transacao)
{
    public async Task RegistrarCotacaoAsync(CotacaoEmitida cotacao, DateTimeOffset agora, CancellationToken cancellationToken)
    {
        await using var comando = Comando(
            """
            INSERT INTO cotacoes (id, transferencia_id, moeda_origem, moeda_destino, taxa, valor_origem, valor_destino, emitida_em, valida_ate)
            VALUES (@id, @transferencia, @origem, @destino, @taxa, @valorOrigem, @valorDestino, @agora, @validaAte)
            """);
        comando.Parameters.AddWithValue("id", cotacao.CotacaoId);
        comando.Parameters.AddWithValue("transferencia", cotacao.TransferenciaId);
        comando.Parameters.AddWithValue("origem", cotacao.MoedaOrigem);
        comando.Parameters.AddWithValue("destino", cotacao.MoedaDestino);
        comando.Parameters.AddWithValue("taxa", cotacao.Taxa);
        comando.Parameters.AddWithValue("valorOrigem", cotacao.ValorOrigem);
        comando.Parameters.AddWithValue("valorDestino", cotacao.ValorDestino);
        comando.Parameters.AddWithValue("agora", agora);
        comando.Parameters.AddWithValue("validaAte", cotacao.ValidaAte);
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }
}
