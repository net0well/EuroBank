using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EuroBank.Shared.Domain;
using EuroBank.Transferencias.Api.Tests.Fixtures;
using EuroBank.Transferencias.Domain.Entities;
using EuroBank.Transferencias.Domain.Repositories;
using EuroBank.Transferencias.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static EuroBank.Transferencias.Api.Tests.Fixtures.ClienteTransferencias;

namespace EuroBank.Transferencias.Api.Tests.Endpoints;

/// <summary>POST /transferencias-externas/{id}/reconciliacao sobre uma transferência Divergente.</summary>
public sealed class ReconciliacaoTestes(PostgresFixture banco)
{
    private readonly ClienteTransferencias _cliente = new(banco.Api.CriarClienteAutenticado());

    private readonly HttpClient _http = banco.Api.CriarClienteAutenticado();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private Task<HttpResponseMessage> ReconciliarAsync(Guid id) =>
        _http.PostAsync($"/transferencias-externas/{id}/reconciliacao", content: null, Ct);

    [Fact]
    public async Task Divergente_PedeAContasODebitoDeNovo()
    {
        var conta = (await banco.NovaContaAsync()).Id;
        var id = await DivergenteAsync(conta);

        var reconciliando = await LerAsync(await ReconciliarAsync(id), HttpStatusCode.Accepted);

        reconciliando.Estado.ShouldBe("Reconciliando");
        reconciliando.Motivo.ShouldBe("ConfirmadaAposEstorno");
        await using var contexto = banco.CriarContexto();
        var comando = (await contexto.Outbox
                .Where(m => m.IdAgregado == conta.ToString() && m.TipoEvento == "DebitarReconciliacao")
                .ToListAsync(Ct))
            .ShouldHaveSingleItem();
        comando.TipoAgregado.ShouldBe("contas.comandos");
        using var payload = JsonDocument.Parse(comando.Payload);
        payload.RootElement.GetProperty("transferenciaId").GetGuid().ShouldBe(id);
        payload.RootElement.GetProperty("contaId").GetGuid().ShouldBe(conta);
    }

    [Fact]
    public async Task Repetida_RespondeAMesmaReconciliacaoSemNovoComando()
    {
        var conta = (await banco.NovaContaAsync()).Id;
        var id = await DivergenteAsync(conta);
        await LerAsync(await ReconciliarAsync(id), HttpStatusCode.Accepted);

        var repetida = await LerAsync(await ReconciliarAsync(id), HttpStatusCode.Accepted);

        repetida.Estado.ShouldBe("Reconciliando");
        await using var contexto = banco.CriarContexto();
        (await contexto.Outbox.CountAsync(m => m.IdAgregado == conta.ToString() && m.TipoEvento == "DebitarReconciliacao", Ct))
            .ShouldBe(1);
    }

    [Fact]
    public async Task Divergente_ApareceNaListagemPorStatus()
    {
        var conta = (await banco.NovaContaAsync()).Id;
        var id = await DivergenteAsync(conta);

        var resposta = await _cliente.GetAsync($"/transferencias?contaOrigemId={conta}&status=Divergente");

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        var pagina = await resposta.Content.ReadFromJsonAsync<JsonElement>(Ct);
        pagina.GetProperty("itens")[0].GetProperty("id").GetGuid().ShouldBe(id);
    }

    [Fact]
    public async Task TransferenciaQueNaoEstaDivergente_Responde409()
    {
        var transferencia = await _cliente.SolicitarComSucessoAsync(Pedido());

        await DeveSerProblemaAsync(await ReconciliarAsync(transferencia.Id), HttpStatusCode.Conflict, "reconciliacao_nao_permitida");
    }

    [Fact]
    public async Task ContaDeOrigemEncerrada_Responde422()
    {
        var conta = (await banco.NovaContaAsync()).Id;
        var id = await DivergenteAsync(conta);
        await using (var contexto = banco.CriarContexto())
        {
            await contexto.ContasConhecidas
                .Where(c => c.Id == conta)
                .ExecuteUpdateAsync(c => c.SetProperty(x => x.EncerradaEm, DateTimeOffset.UtcNow), Ct);
        }

        await DeveSerProblemaAsync(await ReconciliarAsync(id), HttpStatusCode.UnprocessableEntity, "conta_origem_encerrada");
        (await _cliente.ObterAsync(id)).Estado.ShouldBe("Divergente");
    }

    [Fact]
    public async Task Inexistente_Responde404()
    {
        await DeveSerProblemaAsync(await ReconciliarAsync(Guid.CreateVersion7()), HttpStatusCode.NotFound, "transferencia_nao_encontrada");
    }

    /// <summary>
    /// Leva uma externa até Divergente como o worker faria: débito, recusa do banco, estorno e, por fim, a confirmação
    /// tardia do banco.
    /// </summary>
    private async Task<Guid> DivergenteAsync(Guid conta)
    {
        var id = (await _cliente.SolicitarComSucessoAsync(Pedido(conta))).Id;
        await AplicarAsync(id, (transferencia, politica, agora) =>
        {
            transferencia.RegistrarDebitoRealizado(politica, agora);
            transferencia.RegistrarLiquidacaoRecusada(politica, agora);
            transferencia.RegistrarEstornoRealizado(agora);
            transferencia.RegistrarLiquidacaoConfirmada(agora);
        });
        return id;
    }

    private async Task AplicarAsync(Guid id, Action<Transferencia, PoliticaSaga, DateTimeOffset> aplicar)
    {
        await using var escopo = banco.Api.Services.CreateAsyncScope();
        var servicos = escopo.ServiceProvider;
        var transferencia = await servicos.GetRequiredService<IRepositorioTransferencias>().ObterPorIdAsync(new TransferenciaId(id), Ct);
        aplicar(transferencia!, servicos.GetRequiredService<PoliticaSaga>(), DateTimeOffset.UtcNow);
        await servicos.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(Ct);
    }
}
