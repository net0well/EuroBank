using System.Diagnostics;
using System.Net;
using EuroBank.Transferencias.Api.Tests.Fixtures;
using EuroBank.Transferencias.Application.Commands;
using EuroBank.Transferencias.Application.Services;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using static EuroBank.Transferencias.Api.Tests.Fixtures.ClienteTransferencias;

namespace EuroBank.Transferencias.Api.Tests.Endpoints;

/// <summary>
/// A Idempotency-Key reservada no Redis antes do Postgres: pedidos repetidos em paralelo criam uma transferência só,
/// com o Redis ligado e desligado; um repetido espera o primeiro terminar.
/// </summary>
public sealed class IdempotenciaRedisTestes(PostgresFixture banco)
{
    private readonly ClienteTransferencias _cliente = new(banco.Api.CriarClienteAutenticado());

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static RedisKey Chave(string chave) => $"idempotencia:transferencias:{chave}";

    /// <summary>A impressão digital que a API calcula para <see cref="ClienteTransferencias.Pedido"/>.</summary>
    private static string HashDoPedido(Guid conta) => ImpressaoDigitalPedido.Calcular(
        new SolicitarTransferenciaExterna(null, conta, IbanComum, "DEUTDEFF", "Bruno Externo", 100m, null, "Aluguel", null));

    private async Task<int> TransferenciasComAChaveAsync(string chave)
    {
        await using var contexto = banco.CriarContexto();
        return await contexto.Transferencias.CountAsync(t => t.IdempotencyKey == chave, Ct);
    }

    [Fact]
    public async Task EmParalelo_ComRedis_CriaUmaSo_ETodosRecebemAMesma()
    {
        var conta = (await banco.NovaContaAsync()).Id;
        var chave = Guid.CreateVersion7().ToString();

        var respostas = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => _cliente.SolicitarAsync(Pedido(conta), chave)));

        respostas.ShouldAllBe(resposta => resposta.StatusCode == HttpStatusCode.Accepted);
        var ids = await Task.WhenAll(respostas.Select(async resposta => (await LerAsync(resposta, HttpStatusCode.Accepted)).Id));
        ids.Distinct().ShouldHaveSingleItem();
        (await TransferenciasComAChaveAsync(chave)).ShouldBe(1);
        (await banco.Redis.GetDatabase().HashGetAsync(Chave(chave), "estado")).ToString().ShouldBe("concluida");
        (await banco.Redis.GetDatabase().KeyTimeToLiveAsync(Chave(chave))).ShouldNotBeNull().ShouldBeGreaterThan(TimeSpan.FromHours(23));
    }

    [Fact]
    public async Task EmParalelo_ComRedisFora_OPostgresAindaGaranteUmaSo()
    {
        await using var semRedis = banco.CriarApi(PostgresFixture.RedisForaDoAr);
        var cliente = new ClienteTransferencias(semRedis.CriarClienteAutenticado());
        var conta = (await banco.NovaContaAsync()).Id;
        var chave = Guid.CreateVersion7().ToString();

        var respostas = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => cliente.SolicitarAsync(Pedido(conta), chave)));

        respostas.ShouldAllBe(resposta => resposta.StatusCode == HttpStatusCode.Accepted || resposta.StatusCode == HttpStatusCode.Conflict);
        (await TransferenciasComAChaveAsync(chave)).ShouldBe(1);
        (await cliente.SolicitarAsync(Pedido(conta), chave)).StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task OutroPedidoIgualEmAndamento_Espera2s_EResponde409ComRetryAfter()
    {
        var conta = (await banco.NovaContaAsync()).Id;
        var chave = Guid.CreateVersion7().ToString();
        await banco.Redis.GetDatabase().HashSetAsync(Chave(chave), [new HashEntry("estado", "andamento"), new HashEntry("hash", HashDoPedido(conta))]);
        var relogio = Stopwatch.StartNew();

        var resposta = await _cliente.SolicitarAsync(Pedido(conta), chave);

        relogio.Elapsed.ShouldBeGreaterThanOrEqualTo(TimeSpan.FromSeconds(2));
        await DeveSerProblemaAsync(resposta, HttpStatusCode.Conflict, "idempotency_key_em_andamento");
        resposta.Headers.RetryAfter!.Delta.ShouldBe(TimeSpan.FromSeconds(1));
        (await TransferenciasComAChaveAsync(chave)).ShouldBe(0);
    }

    [Fact]
    public async Task OutroPedidoComOutroCorpoEmAndamento_Responde422NaHora()
    {
        var conta = (await banco.NovaContaAsync()).Id;
        var chave = Guid.CreateVersion7().ToString();
        await banco.Redis.GetDatabase().HashSetAsync(Chave(chave), [new HashEntry("estado", "andamento"), new HashEntry("hash", "de-outro-pedido")]);
        var relogio = Stopwatch.StartNew();

        await DeveSerProblemaAsync(await _cliente.SolicitarAsync(Pedido(conta), chave), HttpStatusCode.UnprocessableEntity, "idempotency_key_reutilizada");

        relogio.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task OutroPedidoTerminaDuranteAEspera_DevolveATransferenciaDele()
    {
        var conta = (await banco.NovaContaAsync()).Id;
        var chave = Guid.CreateVersion7().ToString();
        var primeira = await LerAsync(await _cliente.SolicitarAsync(Pedido(conta), chave), HttpStatusCode.Accepted);
        var redis = banco.Redis.GetDatabase();

        // Como se a primeira ainda estivesse gravando: volta a key para andamento e a conclui 300 ms depois.
        await redis.HashSetAsync(Chave(chave), "estado", "andamento");
        var conclusao = Task.Run(
            async () =>
            {
                await Task.Delay(300, Ct);
                await redis.HashSetAsync(Chave(chave), "estado", "concluida");
            },
            Ct);

        var segunda = await LerAsync(await _cliente.SolicitarAsync(Pedido(conta), chave), HttpStatusCode.Accepted);
        await conclusao;

        segunda.Id.ShouldBe(primeira.Id);
        (await TransferenciasComAChaveAsync(chave)).ShouldBe(1);
    }

    [Fact]
    public async Task PedidoQueFalha_SoltaAChave()
    {
        var chave = Guid.CreateVersion7().ToString();

        await DeveSerProblemaAsync(
            await _cliente.SolicitarAsync(Pedido(Guid.CreateVersion7()), chave), HttpStatusCode.UnprocessableEntity, "conta_origem_desconhecida");

        (await banco.Redis.GetDatabase().KeyExistsAsync(Chave(chave))).ShouldBeFalse();
    }
}
