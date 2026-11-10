using System.Net;
using System.Text.Json;
using EuroBank.Shared.Domain;
using EuroBank.Shared.Enums;
using EuroBank.Shared.ValueObjects;
using EuroBank.Transferencias.Api.Tests.Fixtures;
using EuroBank.Transferencias.Application.Services;
using EuroBank.Transferencias.Domain.Repositories;
using EuroBank.Transferencias.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static EuroBank.Transferencias.Api.Tests.Fixtures.ClienteTransferencias;

namespace EuroBank.Transferencias.Api.Tests.Endpoints;

/// <summary>Pedidos com câmbio: a cotação vem antes do débito, e as contas vêm da projeção local.</summary>
public sealed class CambioTestes(PostgresFixture banco)
{
    private readonly ClienteTransferencias _cliente = new(banco.Api.CriarClienteAutenticado());

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<List<(string TipoAgregado, string Tipo, string Payload)>> ComandosAsync(params Guid[] chaves)
    {
        var textos = chaves.Select(chave => chave.ToString()).ToArray();
        await using var contexto = banco.CriarContexto();
        return (await contexto.Outbox
                .Where(mensagem => textos.Contains(mensagem.IdAgregado))
                .OrderBy(mensagem => mensagem.Sequencia)
                .ToListAsync(Ct))
            .Select(mensagem => (mensagem.TipoAgregado, mensagem.TipoEvento, mensagem.Payload))
            .ToList();
    }

    [Fact]
    public async Task ExternaEmOutraMoeda_ComecaCotando_EPedeACotacaoAoCambio()
    {
        var conta = await banco.NovaContaAsync();

        var transferencia = await _cliente.SolicitarComSucessoAsync(
            Pedido(conta.Id, valor: 100m, moedaDestino: "USD", moedaDoValor: "USD"));

        transferencia.Estado.ShouldBe("Cotando");
        transferencia.MoedaOrigem.ShouldBe("EUR");
        transferencia.MoedaDestino.ShouldBe("USD");
        transferencia.ValorDebitado.ShouldBeNull();
        var (tipoAgregado, tipo, payload) = (await ComandosAsync(transferencia.Id)).ShouldHaveSingleItem();
        tipoAgregado.ShouldBe("cambio.comandos");
        tipo.ShouldBe("CotarCambio");
        using var json = JsonDocument.Parse(payload);
        json.RootElement.GetProperty("moedaOrigem").GetString().ShouldBe("EUR");
        json.RootElement.GetProperty("moedaDestino").GetString().ShouldBe("USD");
        json.RootElement.GetProperty("moedaDoValor").GetString().ShouldBe("USD");
        json.RootElement.GetProperty("valor").GetDecimal().ShouldBe(100m);
    }

    [Fact]
    public async Task CambioInterno_CotaEDepoisDebitaACotacaoComOsDadosDoDestino()
    {
        var origem = await banco.NovaContaAsync();
        var destino = await banco.NovaContaAsync(Moeda.USD);

        var transferencia = await LerAsync(
            await _cliente.SolicitarCambioAsync(PedidoCambio(origem.Id, destino.Iban, 100m, "USD")), HttpStatusCode.Accepted);

        transferencia.Tipo.ShouldBe("CambioInterno");
        transferencia.Estado.ShouldBe("Cotando");
        transferencia.NomeBeneficiario.ShouldBe(destino.Titular);

        await RegistrarCotacaoAsync(transferencia.Id);

        var cotada = await _cliente.ObterAsync(transferencia.Id);
        cotada.Estado.ShouldBe("AguardandoDebito");
        cotada.ValorDebitado.ShouldBe(93.53m);
        cotada.ValorDestino.ShouldBe(100m);
        cotada.Cotacao.ShouldNotBeNull().Taxa.ShouldBe(1.0692m);
        var debito = (await ComandosAsync(origem.Id)).ShouldHaveSingleItem();
        debito.Tipo.ShouldBe("DebitarTransferenciaCambio");
        using var json = JsonDocument.Parse(debito.Payload);
        json.RootElement.GetProperty("valor").GetDecimal().ShouldBe(93.53m);
        json.RootElement.GetProperty("tarifa").GetDecimal().ShouldBe(2.50m);
        json.RootElement.GetProperty("moedaDestino").GetString().ShouldBe("USD");
        json.RootElement.GetProperty("valorDestino").GetDecimal().ShouldBe(100m);
        json.RootElement.GetProperty("taxa").GetDecimal().ShouldBe(1.0692m);
    }

    [Fact]
    public async Task ContaDeOrigemForaDaProjecao_Responde422()
    {
        await DeveSerProblemaAsync(
            await _cliente.SolicitarAsync(Pedido(Guid.CreateVersion7()), Guid.CreateVersion7().ToString()),
            HttpStatusCode.UnprocessableEntity,
            "conta_origem_desconhecida");
    }

    [Fact]
    public async Task CambioInterno_PedidosInvalidos_Respondem422()
    {
        var origem = await banco.NovaContaAsync();
        var mesmaMoeda = await banco.NovaContaAsync();
        var emDolar = await banco.NovaContaAsync(Moeda.USD);

        await DeveSerProblemaAsync(
            await _cliente.SolicitarCambioAsync(PedidoCambio(origem.Id, "PT50EURO9999999999999")),
            HttpStatusCode.UnprocessableEntity,
            "conta_destino_desconhecida");
        await DeveSerProblemaAsync(
            await _cliente.SolicitarCambioAsync(PedidoCambio(origem.Id, IbanComum)),
            HttpStatusCode.UnprocessableEntity,
            "destino_nao_eurobank");
        await DeveSerProblemaAsync(
            await _cliente.SolicitarCambioAsync(PedidoCambio(origem.Id, mesmaMoeda.Iban)),
            HttpStatusCode.UnprocessableEntity,
            "cambio_na_mesma_moeda");
        await DeveSerProblemaAsync(
            await _cliente.SolicitarCambioAsync(PedidoCambio(origem.Id, emDolar.Iban, moedaDoValor: "GBP")),
            HttpStatusCode.UnprocessableEntity,
            "moeda_do_valor_invalida");
    }

    /// <summary>Faz o que o worker fará ao receber a CotacaoEmitida do câmbio, pelos serviços da própria API.</summary>
    private async Task RegistrarCotacaoAsync(Guid id)
    {
        await using var escopo = banco.Api.Services.CreateAsyncScope();
        var servicos = escopo.ServiceProvider;
        var transferencia = (await servicos.GetRequiredService<IRepositorioTransferencias>()
            .ObterPorIdAsync(new TransferenciaId(id), Ct))!;
        var agora = DateTimeOffset.UtcNow;
        transferencia.RegistrarCotacaoEmitida(
            new CotacaoTravada(Guid.CreateVersion7(), 1.0692m, agora.AddMinutes(10)),
            Dinheiro.De(93.53m, transferencia.MoedaOrigem),
            Dinheiro.De(100m, transferencia.MoedaDestino),
            await servicos.GetRequiredService<ServicoInicioTransferencia>().ConsumoDoDiaAsync(transferencia, Ct),
            servicos.GetRequiredService<PoliticaSaga>(),
            agora);
        await servicos.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(Ct);
    }
}
