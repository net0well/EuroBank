using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace EuroBank.Antifraude.Infrastructure.MachineLearning;

/// <summary>
/// Troca o modelo sem reiniciar: uma conexão dedicada faz LISTEN no canal antifraude_modelos, que um gatilho em
/// versoes_modelo avisa a cada mudança de status (o aviso chega quando a transação confirma). Recarrega ao conectar
/// (o que mudou antes do LISTEN não se perde), a cada aviso e, sem aviso, a cada minuto, o que também percebe uma
/// conexão que caiu sem avisar. Conexão perdida: tenta de novo a cada 5 s, com os modelos em memória pontuando.
/// </summary>
public sealed class OuvinteModelos(
    IConfiguration configuracao,
    CarregadorModelos carregador,
    ILogger<OuvinteModelos> logger) : BackgroundService
{
    public const string Canal = "antifraude_modelos";

    private static readonly TimeSpan Conferencia = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan EsperaReconexao = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var conexaoBanco = configuracao.GetConnectionString("Antifraude")
            ?? throw new InvalidOperationException("ConnectionStrings:Antifraude não configurada.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await OuvirAsync(conexaoBanco, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception erro) when (erro is not OperationCanceledException)
            {
                logger.LogWarning(erro, "Sem LISTEN em {Canal}; tentando de novo em {Espera}", Canal, EsperaReconexao);
                try
                {
                    await Task.Delay(EsperaReconexao, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }

    private async Task OuvirAsync(string conexaoBanco, CancellationToken cancellationToken)
    {
        await using var conexao = new NpgsqlConnection(conexaoBanco);
        await conexao.OpenAsync(cancellationToken);
        conexao.Notification += (_, aviso) =>
            logger.LogInformation("Aviso de troca de modelo (versão {Versao}): recarregando", aviso.Payload);
        await using (var comando = new NpgsqlCommand($"LISTEN {Canal}", conexao))
        {
            await comando.ExecuteNonQueryAsync(cancellationToken);
        }

        await carregador.RecarregarAsync(cancellationToken);
        while (!cancellationToken.IsCancellationRequested)
        {
            if (!await conexao.WaitAsync(Conferencia, cancellationToken))
            {
                // Um minuto sem aviso: confere a conexão e o banco (barato: só lê os status).
                await using var ping = new NpgsqlCommand("SELECT 1", conexao);
                await ping.ExecuteScalarAsync(cancellationToken);
            }

            await carregador.RecarregarAsync(cancellationToken);
        }
    }
}
