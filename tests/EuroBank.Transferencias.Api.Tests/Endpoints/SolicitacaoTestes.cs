using System.Net;
using System.Text.Json;
using EuroBank.Transferencias.Api.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using static EuroBank.Transferencias.Api.Tests.Fixtures.ClienteTransferencias;

namespace EuroBank.Transferencias.Api.Tests.Endpoints;

public sealed class SolicitacaoTestes(PostgresFixture banco)
{
    private readonly ClienteTransferencias _cliente = new(banco.Api.CriarClienteAutenticado());

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ParaJa_Responde202ComecaASagaEGravaODebitoNaOutbox()
    {
        var conta = Guid.CreateVersion7();

        var resposta = await _cliente.SolicitarAsync(Pedido(conta), Guid.CreateVersion7().ToString());

        var transferencia = await LerAsync(resposta, HttpStatusCode.Accepted);
        resposta.Headers.Location!.ToString().ShouldBe($"/transferencias/{transferencia.Id}");
        transferencia.Estado.ShouldBe("AguardandoDebito");
        transferencia.Tarifa.ShouldBe(2.50m);
        transferencia.Moeda.ShouldBe("EUR");
        transferencia.TentativasNoPasso.ShouldBe(1);
        transferencia.PrazoDoPasso.ShouldNotBeNull();

        await using var contexto = banco.CriarContexto();
        var comando = (await contexto.Outbox.Where(m => m.IdAgregado == conta.ToString()).ToListAsync(Ct)).ShouldHaveSingleItem();
        comando.TipoAgregado.ShouldBe("contas.comandos");
        comando.TipoEvento.ShouldBe("DebitarTransferenciaExterna");
        using var payload = JsonDocument.Parse(comando.Payload);
        payload.RootElement.GetProperty("transferenciaId").GetGuid().ShouldBe(transferencia.Id);
        payload.RootElement.GetProperty("contaId").GetGuid().ShouldBe(conta);
        payload.RootElement.GetProperty("valor").GetDecimal().ShouldBe(100m);
        payload.RootElement.GetProperty("tarifa").GetDecimal().ShouldBe(2.50m);
        payload.RootElement.GetProperty("ibanDestino").GetString().ShouldBe(IbanComum);
        payload.RootElement.GetProperty("nomeBeneficiario").GetString().ShouldBe("Bruno Externo");
    }

    [Fact]
    public async Task ParaDepois_FicaAgendadaSemComandoNaOutbox()
    {
        var conta = Guid.CreateVersion7();
        var amanha = DateTimeOffset.UtcNow.AddDays(1);

        var transferencia = await _cliente.SolicitarComSucessoAsync(Pedido(conta, executarEm: amanha));

        transferencia.Estado.ShouldBe("Agendada");
        transferencia.ExecutarEmEfetivo.ShouldBe(amanha, TimeSpan.FromMilliseconds(1));
        await using var contexto = banco.CriarContexto();
        (await contexto.Outbox.CountAsync(m => m.IdAgregado == conta.ToString(), Ct)).ShouldBe(0);
    }

    [Fact]
    public async Task MesmaIdempotencyKeyEMesmoCorpo_DevolveAMesmaTransferencia()
    {
        var chave = Guid.CreateVersion7().ToString();
        var conta = Guid.CreateVersion7();

        var primeira = await LerAsync(await _cliente.SolicitarAsync(Pedido(conta), chave), HttpStatusCode.Accepted);
        var segunda = await LerAsync(await _cliente.SolicitarAsync(Pedido(conta), chave), HttpStatusCode.Accepted);

        segunda.Id.ShouldBe(primeira.Id);
        await using var contexto = banco.CriarContexto();
        (await contexto.Transferencias.CountAsync(t => t.ContaOrigemId == conta, Ct)).ShouldBe(1);
        (await contexto.Outbox.CountAsync(m => m.IdAgregado == conta.ToString(), Ct)).ShouldBe(1);
    }

    [Fact]
    public async Task MesmaIdempotencyKeyComOutroCorpo_Responde422()
    {
        var chave = Guid.CreateVersion7().ToString();
        var conta = Guid.CreateVersion7();
        await LerAsync(await _cliente.SolicitarAsync(Pedido(conta, valor: 100m), chave), HttpStatusCode.Accepted);

        await DeveSerProblemaAsync(
            await _cliente.SolicitarAsync(Pedido(conta, valor: 200m), chave),
            HttpStatusCode.UnprocessableEntity,
            "idempotency_key_reutilizada");
    }

    [Fact]
    public async Task SemIdempotencyKey_Responde400()
    {
        await DeveSerProblemaAsync(
            await _cliente.SolicitarAsync(Pedido(), idempotencyKey: null),
            HttpStatusCode.BadRequest,
            "idempotency_key_obrigatoria");
    }

    [Theory]
    [InlineData("PT50EURO0000000000001", "DEUTDEFF", 100, HttpStatusCode.UnprocessableEntity, "destino_eurobank")]
    [InlineData(IbanComum, null, 100, HttpStatusCode.UnprocessableEntity, "bic_obrigatorio")]
    [InlineData("DE89", "DEUTDEFF", 100, HttpStatusCode.BadRequest, "iban_destino_invalido")]
    [InlineData(IbanComum, "DEUTDEFF", 0, HttpStatusCode.BadRequest, "valor_invalido")]
    [InlineData(IbanComum, "DEUTDEFF", 10.001, HttpStatusCode.BadRequest, "valor_invalido")]
    public async Task PedidoInvalido_RespondeComOProblemaCerto(
        string iban,
        string? bic,
        double valor,
        HttpStatusCode status,
        string codigo)
    {
        await DeveSerProblemaAsync(
            await _cliente.SolicitarAsync(Pedido(iban: iban, bic: bic, valor: (decimal)valor), Guid.CreateVersion7().ToString()),
            status,
            codigo);
    }

    [Fact]
    public async Task PassandoDoLimiteDiario_TerminaRecusada_EATarifaNaoConta()
    {
        var conta = Guid.CreateVersion7();
        (await _cliente.SolicitarComSucessoAsync(Pedido(conta, valor: 4_900m))).Estado.ShouldBe("AguardandoDebito");
        (await _cliente.SolicitarComSucessoAsync(Pedido(conta, valor: 100m))).Estado.ShouldBe("AguardandoDebito");

        var acimaDoLimite = await _cliente.SolicitarComSucessoAsync(Pedido(conta, valor: 0.01m));

        acimaDoLimite.Estado.ShouldBe("Recusada");
        acimaDoLimite.Motivo.ShouldBe("LimiteDiarioExcedido");
    }

    [Fact]
    public async Task LimiteDiario_ConcorrenteNaMesmaConta_NuncaPassaDoLimite()
    {
        var conta = Guid.CreateVersion7();

        var respostas = await Task.WhenAll(Enumerable.Range(0, 6)
            .Select(_ => _cliente.SolicitarComSucessoAsync(Pedido(conta, valor: 1_000m))));

        respostas.Count(t => t.Estado == "AguardandoDebito").ShouldBe(5);
        respostas.Count(t => t.Motivo == "LimiteDiarioExcedido").ShouldBe(1);
    }
}
