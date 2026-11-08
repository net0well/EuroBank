using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EuroBank.Extrato.Application.TransferObjects;
using EuroBank.Extrato.Domain.Exceptions;
using EuroBank.Extrato.Worker.Tests.Fixtures;
using EuroBank.Shared.Exceptions;
using EuroBank.Shared.TransferObjects;

namespace EuroBank.Extrato.Worker.Tests.Handlers;

public sealed class ProjecaoExtratoTestes(ExtratoWorkerFixture worker)
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 6, 12, 0, 0, TimeSpan.Zero);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<Guid> AbrirAsync(string titular = "Ana")
    {
        var contaId = Guid.CreateVersion7();
        await worker.EntregarAsync(new EventoContaAberta(contaId, "PT53EURO0000000000001", titular, 1, Agora));
        return contaId;
    }

    private Task<ExtratoResposta?> ObterAsync(Guid contaId, string consulta = "") =>
        worker.Http.GetFromJsonAsync<ExtratoResposta>($"/extratos/{contaId}{consulta}", Ct);

    [Fact]
    public async Task SequenciaDeEventos_MontaOExtratoComSaldoStatusELancamentosDoMaisRecente()
    {
        var contaId = await AbrirAsync();
        var transferenciaId = Guid.CreateVersion7();

        await worker.EntregarAsync(new EventoDepositoRealizado(contaId, 100m, 100m, 2, Agora));
        await worker.EntregarAsync(new EventoSaqueRealizado(contaId, 40m, 60m, 3, Agora));
        await worker.EntregarAsync(new EventoTransferenciaEnviada(
            contaId, transferenciaId, "PT26EURO0000000000002", 30m, 30m, 4, Agora));
        await worker.EntregarAsync(new EventoTransferenciaRecebida(
            contaId, transferenciaId, "PT96EURO0000000000003", 5m, 35m, 5, Agora));
        await worker.EntregarAsync(new EventoContaBloqueada(contaId, 6, Agora));

        var extrato = (await ObterAsync(contaId))!;

        extrato.Titular.ShouldBe("Ana");
        extrato.Iban.ShouldBe("PT53EURO0000000000001");
        extrato.Status.ShouldBe("Bloqueada");
        extrato.SaldoAtual.ShouldBe(35m);
        extrato.Moeda.ShouldBe("EUR");
        extrato.UltimaVersao.ShouldBe(6);
        extrato.Total.ShouldBe(4);
        extrato.Lancamentos.Select(l => l.Versao).ShouldBe([5, 4, 3, 2]);
        extrato.Lancamentos.Select(l => l.Tipo)
            .ShouldBe(["TransferenciaRecebida", "TransferenciaEnviada", "Saque", "Deposito"]);
        extrato.Lancamentos[0].Descricao.ShouldBe("Transferência de PT96EURO0000000000003");
        extrato.Lancamentos[0].SaldoApos.ShouldBe(35m);
    }

    [Fact]
    public async Task TransferenciaExternaEEstorno_AparecemNoExtratoComTarifa()
    {
        var contaId = await AbrirAsync();
        var transferenciaId = Guid.CreateVersion7();
        await worker.EntregarAsync(new EventoDepositoRealizado(contaId, 1000m, 1000m, 2, Agora));

        await worker.EntregarAsync(new EventoTransferenciaExternaEnviada(
            contaId, transferenciaId, "DE89370400440532013000", "Bruno", null, 100m, 2.50m, 897.50m, 3, Agora));
        (await ObterAsync(contaId))!.Lancamentos[0].Tarifa.ShouldBe(2.50m);
        await worker.EntregarAsync(new EventoEstornoTransferencia(contaId, transferenciaId, "Bruno", 102.50m, 1000m, 4, Agora));

        var extrato = (await ObterAsync(contaId))!;
        extrato.SaldoAtual.ShouldBe(1000m);
        extrato.Lancamentos.Select(l => l.Tipo).ShouldBe(["EstornoTransferencia", "TransferenciaExternaEnviada", "Deposito"]);
        extrato.Lancamentos[0].Tarifa.ShouldBeNull();
        extrato.Lancamentos[1].Descricao.ShouldBe("Transferência para Bruno (DE89370400440532013000)");
    }

    [Fact]
    public async Task EventoRepetido_NaoAlteraOSaldoNemDuplicaLancamento()
    {
        var contaId = await AbrirAsync();
        var deposito = new EventoDepositoRealizado(contaId, 100m, 100m, 2, Agora);

        await worker.EntregarAsync(deposito);
        await worker.EntregarAsync(deposito);
        await worker.EntregarAsync(new EventoContaAberta(contaId, "PT53EURO0000000000001", "Ana", 1, Agora));

        var extrato = (await ObterAsync(contaId))!;
        extrato.SaldoAtual.ShouldBe(100m);
        extrato.Total.ShouldBe(1);
        extrato.UltimaVersao.ShouldBe(2);
    }

    [Fact]
    public async Task StatusDaConta_AcompanhaBloqueioDesbloqueioEEncerramento()
    {
        var contaId = await AbrirAsync();

        await worker.EntregarAsync(new EventoContaBloqueada(contaId, 2, Agora));
        (await ObterAsync(contaId))!.Status.ShouldBe("Bloqueada");
        await worker.EntregarAsync(new EventoContaDesbloqueada(contaId, 3, Agora));
        (await ObterAsync(contaId))!.Status.ShouldBe("Ativa");
        await worker.EntregarAsync(new EventoContaEncerrada(contaId, 4, Agora));
        (await ObterAsync(contaId))!.Status.ShouldBe("Encerrada");
    }

    [Fact]
    public async Task Paginacao_RetornaOsMaisRecentesPrimeiroComTotal()
    {
        var contaId = await AbrirAsync();
        for (var versao = 2; versao <= 5; versao++)
        {
            await worker.EntregarAsync(new EventoDepositoRealizado(contaId, 1m, versao - 1, versao, Agora));
        }

        var primeira = (await ObterAsync(contaId, "?pagina=1&tamanho=3"))!;
        var segunda = (await ObterAsync(contaId, "?pagina=2&tamanho=3"))!;

        primeira.Lancamentos.Select(l => l.Versao).ShouldBe([5, 4, 3]);
        segunda.Lancamentos.Select(l => l.Versao).ShouldBe([2]);
        segunda.Pagina.ShouldBe(2);
        segunda.Tamanho.ShouldBe(3);
        segunda.Total.ShouldBe(4);
    }

    [Fact]
    public async Task EventoDesconhecido_EIgnoradoSemErro()
    {
        await worker.EntregarAsync(new EventoDesconhecido("ContaPromovidaParaVip"));
    }

    [Fact]
    public async Task MensagemInvalida_LancaExcecaoParaIrAoDeadLetter()
    {
        await Should.ThrowAsync<ExcecaoMensagemInvalida>(
            () => worker.EntregarAsync(new MensagemInvalida("mensagem sem o header tipo")));
    }

    [Fact]
    public async Task EventoDeContaSemExtrato_LancaExcecaoExtratoNaoEncontrado()
    {
        await Should.ThrowAsync<ExcecaoExtratoNaoEncontrado>(
            () => worker.EntregarAsync(new EventoDepositoRealizado(Guid.CreateVersion7(), 1m, 1m, 2, Agora)));
    }

    [Fact]
    public async Task ExtratoInexistente_Retorna404()
    {
        var resposta = await worker.Http.GetAsync($"/extratos/{Guid.CreateVersion7()}", Ct);

        await DeveSerProblemaAsync(resposta, HttpStatusCode.NotFound, "extrato_nao_encontrado");
    }

    [Theory]
    [InlineData("?pagina=0")]
    [InlineData("?tamanho=0")]
    [InlineData("?tamanho=201")]
    public async Task PaginacaoInvalida_Retorna400(string consulta)
    {
        var contaId = await AbrirAsync();

        var resposta = await worker.Http.GetAsync($"/extratos/{contaId}{consulta}", Ct);

        await DeveSerProblemaAsync(resposta, HttpStatusCode.BadRequest, "paginacao_invalida");
    }

    private static async Task DeveSerProblemaAsync(HttpResponseMessage resposta, HttpStatusCode status, string codigo)
    {
        var corpo = await resposta.Content.ReadAsStringAsync(Ct);
        resposta.StatusCode.ShouldBe(status, corpo);
        using var problema = JsonDocument.Parse(corpo);
        problema.RootElement.GetProperty("codigo").GetString().ShouldBe(codigo);
    }

    private sealed record ExtratoResposta(
        Guid ContaId,
        string Iban,
        string Titular,
        string Status,
        decimal SaldoAtual,
        string Moeda,
        int UltimaVersao,
        int Pagina,
        int Tamanho,
        long Total,
        List<LancamentoResposta> Lancamentos);

    private sealed record LancamentoResposta(
        string Tipo, decimal Valor, decimal? Tarifa, decimal SaldoApos, int Versao, DateTimeOffset OcorridoEm, string Descricao);
}
