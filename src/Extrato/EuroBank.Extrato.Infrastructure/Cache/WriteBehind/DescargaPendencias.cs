using System.Text.Json;
using EuroBank.Extrato.Infrastructure.Cache.TransferObjects;
using EuroBank.Extrato.Infrastructure.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace EuroBank.Extrato.Infrastructure.Cache.WriteBehind;

/// <summary>
/// Descarrega no Postgres as pendências do write-behind (stream extrato:pendencias, consumer group descarga). Cada
/// instância do worker é um consumidor: primeiro reivindica o que outra instância deixou parado por mais de 30 s, depois
/// retoma as próprias pendências não confirmadas e então lê as novas. Só confirma (e apaga) depois do commit.
/// </summary>
public sealed class DescargaPendencias(
    IConnectionMultiplexer redis,
    IServiceScopeFactory escopos,
    IOptions<OpcoesCache> opcoes,
    ILogger<DescargaPendencias> logger)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static readonly TimeSpan OciosidadeParaReivindicar = TimeSpan.FromSeconds(30);

    private readonly string _consumidor = Environment.MachineName;

    private bool _grupoCriado;

    /// <summary>Um ciclo: devolve quantas pendências foram gravadas no Postgres.</summary>
    public async Task<int> DescarregarAsync(CancellationToken cancellationToken)
    {
        var banco = redis.GetDatabase();
        await GarantirGrupoAsync(banco);
        var lote = opcoes.Value.LoteDescarga;

        var reivindicadas = await banco.StreamAutoClaimAsync(
            ChavesExtrato.Pendencias, ChavesExtrato.GrupoDescarga, _consumidor, (long)OciosidadeParaReivindicar.TotalMilliseconds, "0-0", lote);
        var gravadas = await GravarAsync(banco, reivindicadas.ClaimedEntries, cancellationToken);

        // "0": as entregues a este consumidor e ainda não confirmadas (o processo caiu entre gravar e confirmar).
        gravadas += await GravarAsync(
            banco, await banco.StreamReadGroupAsync(ChavesExtrato.Pendencias, ChavesExtrato.GrupoDescarga, _consumidor, "0", lote), cancellationToken);

        StreamEntry[] novas;
        do
        {
            novas = await banco.StreamReadGroupAsync(ChavesExtrato.Pendencias, ChavesExtrato.GrupoDescarga, _consumidor, ">", lote);
            gravadas += await GravarAsync(banco, novas, cancellationToken);
        }
        while (novas.Length == lote && !cancellationToken.IsCancellationRequested);

        return gravadas;
    }

    private async Task<int> GravarAsync(IDatabase banco, StreamEntry[] entradas, CancellationToken cancellationToken)
    {
        if (entradas.Length == 0)
        {
            return 0;
        }

        var pendencias = new List<PendenciaExtrato>(entradas.Length);
        foreach (var entrada in entradas)
        {
            var dados = entrada["dados"];
            if (dados.IsNullOrEmpty)
            {
                logger.LogWarning("Pendência {Id} sem dados descartada", entrada.Id);
                continue;
            }

            pendencias.Add(JsonSerializer.Deserialize<PendenciaExtrato>(dados.ToString(), Json)!);
        }

        await using (var escopo = escopos.CreateAsyncScope())
        {
            await escopo.ServiceProvider.GetRequiredService<GravadorExtratosPostgres>().GravarAsync(pendencias, cancellationToken);
        }

        var ids = entradas.Select(entrada => entrada.Id).ToArray();
        await banco.StreamAcknowledgeAsync(ChavesExtrato.Pendencias, ChavesExtrato.GrupoDescarga, ids);
        await banco.StreamDeleteAsync(ChavesExtrato.Pendencias, ids);
        logger.LogDebug("Write-behind: {Quantidade} pendências gravadas no Postgres", pendencias.Count);
        return pendencias.Count;
    }

    private async Task GarantirGrupoAsync(IDatabase banco)
    {
        if (_grupoCriado)
        {
            return;
        }

        try
        {
            await banco.StreamCreateConsumerGroupAsync(ChavesExtrato.Pendencias, ChavesExtrato.GrupoDescarga, "0-0", createStream: true);
        }
        catch (RedisServerException erro) when (erro.Message.StartsWith("BUSYGROUP", StringComparison.Ordinal))
        {
            // O grupo já existe (outra instância ou um ciclo anterior o criou).
        }

        _grupoCriado = true;
    }
}
