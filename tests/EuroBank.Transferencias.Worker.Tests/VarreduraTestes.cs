using EuroBank.Transferencias.Application.Replies;
using EuroBank.Transferencias.Application.Services;
using EuroBank.Transferencias.Domain.Enums;
using EuroBank.Transferencias.Domain.ValueObjects;
using EuroBank.Transferencias.Worker.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace EuroBank.Transferencias.Worker.Tests;

[Collection(nameof(ColecaoWorker))]
public sealed class VarreduraTestes(TransferenciasWorkerFixture worker)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task AgendadaNaHora_ComecaPedindoODebito()
    {
        var id = await worker.CriarAsync(executarEm: worker.Relogio.GetUtcNow().AddMinutes(1));
        (await PendentesAsync()).ShouldNotContain(id);

        worker.Relogio.Advance(TimeSpan.FromMinutes(1));

        (await PendentesAsync()).ShouldContain(id);
        (await AvancarAsync(id)).ShouldBe(ResultadoPasso.Aplicado);
        (await worker.ObterAsync(id)).Estado.ShouldBe(EstadoTransferencia.AguardandoDebito);
        var conta = (await worker.ObterAsync(id)).ContaOrigemId;
        (await worker.ComandosAsync(id, conta)).ShouldBe(["DebitarTransferenciaExterna"]);
    }

    [Fact]
    public async Task PrazoDaLiquidacaoVencido_ReenviaOComando()
    {
        var id = await worker.CriarAsync();
        var conta = (await worker.ObterAsync(id)).ContaOrigemId;
        await worker.EntregarAsync(new DebitoRealizado(id.Valor, conta, 100m, 2.50m));
        var politica = worker.Servicos.GetRequiredService<PoliticaSaga>();

        worker.Relogio.Advance(politica.PrazoLiquidacao);

        (await AvancarAsync(id)).ShouldBe(ResultadoPasso.Aplicado);
        var transferencia = await worker.ObterAsync(id);
        transferencia.TentativasNoPasso.ShouldBe(2);
        (await worker.ComandosAsync(id, conta)).Count(tipo => tipo == "LiquidarTransferencia").ShouldBe(2);
        (await AvancarAsync(id)).ShouldBe(ResultadoPasso.Ignorado);
    }

    [Fact]
    public async Task LinhaTravadaPorOutraInstancia_EPulada()
    {
        var id = await worker.CriarAsync();
        worker.Relogio.Advance(worker.Servicos.GetRequiredService<PoliticaSaga>().PrazoDebito);

        await using (var outraInstancia = new NpgsqlConnection(worker.ConnectionString))
        {
            await outraInstancia.OpenAsync(Ct);
            await using var transacao = await outraInstancia.BeginTransactionAsync(Ct);
            await using var travar = new NpgsqlCommand("SELECT 1 FROM transferencias WHERE id = @id FOR UPDATE", outraInstancia, transacao);
            travar.Parameters.AddWithValue("id", id.Valor);
            await travar.ExecuteNonQueryAsync(Ct);

            (await AvancarAsync(id)).ShouldBe(ResultadoPasso.Ignorado);
        }

        (await AvancarAsync(id)).ShouldBe(ResultadoPasso.Aplicado);
        (await worker.ObterAsync(id)).TentativasNoPasso.ShouldBe(2);
    }

    private async Task<IReadOnlyList<TransferenciaId>> PendentesAsync()
    {
        await using var escopo = worker.Servicos.CreateAsyncScope();
        return await escopo.ServiceProvider.GetRequiredService<VarreduraTransferencias>().ListarPendentesAsync(500, Ct);
    }

    private async Task<ResultadoPasso> AvancarAsync(TransferenciaId id)
    {
        await using var escopo = worker.Servicos.CreateAsyncScope();
        return await escopo.ServiceProvider.GetRequiredService<VarreduraTransferencias>().AvancarAsync(id, Ct);
    }
}
