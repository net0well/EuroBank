using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EuroBank.Shared.Domain;
using EuroBank.Transferencias.Api.Tests.Fixtures;
using EuroBank.Transferencias.Domain.Repositories;
using EuroBank.Transferencias.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static EuroBank.Transferencias.Api.Tests.Fixtures.ClienteTransferencias;

namespace EuroBank.Transferencias.Api.Tests.Endpoints;

public sealed class ConsultaECancelamentoTestes(PostgresFixture banco)
{
    private readonly ClienteTransferencias _cliente = new(banco.Api.CriarClienteAutenticado());

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Obter_MostraEstadoEHistorico()
    {
        var criada = await _cliente.SolicitarComSucessoAsync(Pedido());

        var transferencia = await _cliente.ObterAsync(criada.Id);

        transferencia.Estado.ShouldBe("AguardandoDebito");
        transferencia.Historico.Select(passo => passo.EstadoApos).ShouldBe(["Agendada", "AguardandoDebito"]);
        transferencia.Historico.Select(passo => passo.Ordem).ShouldBe([1, 2]);
        transferencia.Versao.ShouldBe(2);
    }

    [Fact]
    public async Task Obter_Inexistente_Responde404()
    {
        await DeveSerProblemaAsync(
            await _cliente.GetAsync($"/transferencias/{Guid.CreateVersion7()}"), HttpStatusCode.NotFound, "transferencia_nao_encontrada");
    }

    [Fact]
    public async Task Listar_FiltraPorContaEStatus()
    {
        var conta = (await banco.NovaContaAsync()).Id;
        await _cliente.SolicitarComSucessoAsync(Pedido(conta));
        await _cliente.SolicitarComSucessoAsync(Pedido(conta, executarEm: DateTimeOffset.UtcNow.AddDays(1)));
        await _cliente.SolicitarComSucessoAsync(Pedido());

        var todas = await LerPaginaAsync($"/transferencias?contaOrigemId={conta}");
        var agendadas = await LerPaginaAsync($"/transferencias?contaOrigemId={conta}&status=agendada&tipo=Externa");

        todas.GetProperty("total").GetInt64().ShouldBe(2);
        agendadas.GetProperty("total").GetInt64().ShouldBe(1);
        agendadas.GetProperty("itens")[0].GetProperty("estado").GetString().ShouldBe("Agendada");
    }

    [Theory]
    [InlineData("status=Inventado")]
    [InlineData("status=1")]
    [InlineData("tipo=Interna")]
    public async Task Listar_ComFiltroInvalido_Responde400(string filtro)
    {
        await DeveSerProblemaAsync(await _cliente.GetAsync($"/transferencias?{filtro}"), HttpStatusCode.BadRequest, "filtro_invalido");
    }

    [Fact]
    public async Task CancelarAgendada_TerminaCancelada()
    {
        var agendada = await _cliente.SolicitarComSucessoAsync(Pedido(executarEm: DateTimeOffset.UtcNow.AddDays(1)));

        var cancelada = await LerAsync(await _cliente.CancelarAsync(agendada.Id), HttpStatusCode.Accepted);

        cancelada.Estado.ShouldBe("Cancelada");
        cancelada.Motivo.ShouldBe("CanceladaPeloCliente");
        (await _cliente.ObterAsync(agendada.Id)).Historico.Count.ShouldBe(2);
    }

    [Fact]
    public async Task CancelarComDebitoEmAndamento_SoAnota()
    {
        var emAndamento = await _cliente.SolicitarComSucessoAsync(Pedido());

        var resposta = await LerAsync(await _cliente.CancelarAsync(emAndamento.Id), HttpStatusCode.Accepted);

        resposta.Estado.ShouldBe("AguardandoDebito");
        resposta.CancelamentoSolicitado.ShouldBeTrue();
    }

    [Fact]
    public async Task CancelarDepoisDeAPedirALiquidacao_Responde409()
    {
        var transferencia = await _cliente.SolicitarComSucessoAsync(Pedido());
        await RegistrarDebitoRealizadoAsync(transferencia.Id);

        await DeveSerProblemaAsync(await _cliente.CancelarAsync(transferencia.Id), HttpStatusCode.Conflict, "cancelamento_nao_permitido");
    }

    [Fact]
    public async Task DebitoRealizado_GravaALiquidacaoParaOBancoExternoNaOutbox()
    {
        var transferencia = await _cliente.SolicitarComSucessoAsync(Pedido(valor: 250m));

        await RegistrarDebitoRealizadoAsync(transferencia.Id);

        await using var contexto = banco.CriarContexto();
        var comando = (await contexto.Outbox.Where(m => m.IdAgregado == transferencia.Id.ToString()).ToListAsync(Ct))
            .ShouldHaveSingleItem();
        comando.TipoAgregado.ShouldBe("banco-externo.comandos");
        comando.TipoEvento.ShouldBe("LiquidarTransferencia");
        using var payload = JsonDocument.Parse(comando.Payload);
        payload.RootElement.GetProperty("valor").GetDecimal().ShouldBe(250m);
        payload.RootElement.GetProperty("moeda").GetString().ShouldBe("EUR");
        payload.RootElement.GetProperty("bic").GetString().ShouldBe("DEUTDEFF");
        (await _cliente.ObterAsync(transferencia.Id)).Estado.ShouldBe("AguardandoLiquidacao");
    }

    /// <summary>Faz o que o worker fará ao receber o DebitoRealizado de Contas, pelos serviços da própria API.</summary>
    private async Task RegistrarDebitoRealizadoAsync(Guid id)
    {
        await using var escopo = banco.Api.Services.CreateAsyncScope();
        var servicos = escopo.ServiceProvider;
        var transferencia = await servicos.GetRequiredService<IRepositorioTransferencias>()
            .ObterPorIdAsync(new TransferenciaId(id), Ct);
        transferencia!.RegistrarDebitoRealizado(servicos.GetRequiredService<PoliticaSaga>(), DateTimeOffset.UtcNow);
        await servicos.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(Ct);
    }

    private async Task<JsonElement> LerPaginaAsync(string url)
    {
        var resposta = await _cliente.GetAsync(url);
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK, await resposta.Content.ReadAsStringAsync(Ct));
        return await resposta.Content.ReadFromJsonAsync<JsonElement>(Ct);
    }
}
