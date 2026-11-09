using Npgsql;

namespace EuroBank.Simuladores.Comum;

public static class BancoDeDados
{
    /// <summary>
    /// Cria o database do simulador se ele ainda não existir (o script de init do Postgres só roda num volume
    /// novo, então um laboratório já existente não teria o database).
    /// </summary>
    public static async Task GarantirDatabaseAsync(string connectionString, CancellationToken cancellationToken)
    {
        var destino = new NpgsqlConnectionStringBuilder(connectionString);
        var nome = destino.Database ?? throw new InvalidOperationException("A connection string não informa o database.");
        var manutencao = new NpgsqlConnectionStringBuilder(connectionString) { Database = "postgres" };

        await using var conexao = new NpgsqlConnection(manutencao.ConnectionString);
        await conexao.OpenAsync(cancellationToken);

        await using var existe = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @nome", conexao);
        existe.Parameters.AddWithValue("nome", nome);
        if (await existe.ExecuteScalarAsync(cancellationToken) is not null)
        {
            return;
        }

        // O nome vem da configuração do próprio simulador; CREATE DATABASE não aceita parâmetro.
        await using var criar = new NpgsqlCommand($"CREATE DATABASE \"{nome.Replace("\"", "\"\"", StringComparison.Ordinal)}\"", conexao);
        await criar.ExecuteNonQueryAsync(cancellationToken);
    }
}
