using System.Net;
using System.Text.Json;
using EuroBank.Antifraude.Api.Tests.Fixtures;
using EuroBank.Antifraude.Grpc;
using Microsoft.EntityFrameworkCore;
using static EuroBank.Antifraude.Api.Tests.Fixtures.ClienteAnalista;

namespace EuroBank.Antifraude.Api.Tests.Endpoints;

/// <summary>A fila de revisão pelo REST: listar, detalhar, assumir e decidir, com a decisão indo para a outbox.</summary>
public sealed class CasosEndpointsTestes(AntifraudeFixture antifraude) : IDisposable
{
    private readonly ClienteAnalista _analista = new(antifraude);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static object Decisao(string decisao, string analista = "Marta", string justificativa = "Cliente confirmou por telefone", bool bloquear = false) =>
        new { analista, decisao, justificativa, bloquearIban = bloquear };

    [Fact]
    public async Task Fila_MostraOCasoPendente()
    {
        var (casoId, operacaoId) = await _analista.CasoPendenteAsync();

        var fila = await _analista.GetAsync("/casos?tamanho=200");

        var item = fila.GetProperty("itens").EnumerateArray().Single(caso => caso.GetProperty("id").GetGuid() == casoId);
        item.GetProperty("estado").GetString().ShouldBe("Aberto");
        item.GetProperty("operacaoId").GetGuid().ShouldBe(operacaoId);
        item.GetProperty("scoreFinal").GetInt32().ShouldBe(50);
        item.GetProperty("valor").GetDecimal().ShouldBe(4_600m);
        item.GetProperty("tipoOperacao").GetString().ShouldBe("Externa");
    }

    [Fact]
    public async Task Detalhe_TemAAnaliseComOsSinaisEAAuditoria()
    {
        var (casoId, _) = await _analista.CasoPendenteAsync();

        var caso = await _analista.GetAsync($"/casos/{casoId}");

        caso.GetProperty("estado").GetString().ShouldBe("Aberto");
        var analise = caso.GetProperty("analise");
        analise.GetProperty("decisao").GetString().ShouldBe("EmRevisao");
        analise.GetProperty("sinais").GetArrayLength().ShouldBe(9);
        analise.GetProperty("sinais").EnumerateArray()
            .Where(sinal => sinal.GetProperty("disparou").GetBoolean())
            .Select(sinal => sinal.GetProperty("codigo").GetString())
            .ShouldBe(["ValorAcimaDaMedia", "Esvaziamento"], ignoreOrder: true);
        caso.GetProperty("auditoria")[0].GetProperty("acao").GetString().ShouldBe("Caso aberto");
    }

    [Fact]
    public async Task AssumirEAprovar_FechaOCasoEAAnalise_EPublicaADecisao()
    {
        var (casoId, operacaoId) = await _analista.CasoPendenteAsync();

        var assumido = await OkAsync(await _analista.PostAsync($"/casos/{casoId}/atribuicao", new { analista = "Marta" }));
        assumido.GetProperty("estado").GetString().ShouldBe("EmAnalise");
        assumido.GetProperty("analista").GetString().ShouldBe("Marta");

        var decidido = await OkAsync(await _analista.PostAsync($"/casos/{casoId}/decisao", Decisao("Aprovar")));
        decidido.GetProperty("estado").GetString().ShouldBe("Aprovado");
        decidido.GetProperty("analise").GetProperty("decisao").GetString().ShouldBe("Aprovada");
        decidido.GetProperty("auditoria").EnumerateArray().Select(registro => registro.GetProperty("acao").GetString())
            .ShouldBe(["Caso aberto", "Caso assumido", "Aprovado"]);

        await using var contexto = antifraude.CriarContexto();
        var mensagens = await contexto.Outbox.AsNoTracking()
            .Where(mensagem => mensagem.IdAgregado == operacaoId.ToString())
            .OrderBy(mensagem => mensagem.Sequencia)
            .ToListAsync(Ct);
        mensagens.Select(mensagem => mensagem.TipoEvento).ShouldBe(["CasoAberto", "AnaliseDecidida"]);
        using var payload = JsonDocument.Parse(mensagens[1].Payload);
        payload.RootElement.GetProperty("decisao").GetString().ShouldBe("Aprovada");
        payload.RootElement.GetProperty("origem").GetString().ShouldBe("Analista");
        payload.RootElement.GetProperty("analista").GetString().ShouldBe("Marta");

        var porGrpc = await antifraude.CriarClienteGrpc().ObterAnaliseAsync(
            new ObterAnaliseRequest { AnaliseId = decidido.GetProperty("analise").GetProperty("id").GetString() }, cancellationToken: Ct);
        porGrpc.Decisao.ShouldBe(EuroBank.Antifraude.Grpc.Decisao.Aprovada);
        porGrpc.DecididaEm.ShouldNotBeNull();
    }

    [Fact]
    public async Task RecusarComBloqueio_PoeOIbanNaLista()
    {
        var iban = IbanUnico();
        var (casoId, _) = await _analista.CasoPendenteAsync(iban);
        await _analista.PostAsync($"/casos/{casoId}/atribuicao", new { analista = "Marta" });

        var decidido = await OkAsync(await _analista.PostAsync($"/casos/{casoId}/decisao", Decisao("Recusar", bloquear: true)));

        decidido.GetProperty("estado").GetString().ShouldBe("Recusado");
        decidido.GetProperty("bloquearIban").GetBoolean().ShouldBeTrue();
        var bloqueados = await _analista.GetAsync("/listas/bloqueio");
        var bloqueio = bloqueados.EnumerateArray().Single(item => item.GetProperty("iban").GetString() == iban);
        bloqueio.GetProperty("bloqueadoPor").GetString().ShouldBe("Marta");

        (await _analista.DeleteAsync($"/listas/bloqueio/{iban}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DecidirSemAssumir_422()
    {
        var (casoId, _) = await _analista.CasoPendenteAsync();

        await DeveSerProblemaAsync(
            await _analista.PostAsync($"/casos/{casoId}/decisao", Decisao("Aprovar")), HttpStatusCode.UnprocessableEntity, "caso_nao_assumido");
    }

    [Fact]
    public async Task CasoDeOutroAnalista_409()
    {
        var (casoId, _) = await _analista.CasoPendenteAsync();
        await _analista.PostAsync($"/casos/{casoId}/atribuicao", new { analista = "Marta" });

        await DeveSerProblemaAsync(
            await _analista.PostAsync($"/casos/{casoId}/atribuicao", new { analista = "Paulo" }), HttpStatusCode.Conflict, "caso_de_outro_analista");
        await DeveSerProblemaAsync(
            await _analista.PostAsync($"/casos/{casoId}/decisao", Decisao("Aprovar", analista: "Paulo")), HttpStatusCode.Conflict, "caso_de_outro_analista");
    }

    [Fact]
    public async Task DecisaoSemJustificativaOuInvalida_400()
    {
        var (casoId, _) = await _analista.CasoPendenteAsync();
        await _analista.PostAsync($"/casos/{casoId}/atribuicao", new { analista = "Marta" });

        await DeveSerProblemaAsync(
            await _analista.PostAsync($"/casos/{casoId}/decisao", Decisao("Aprovar", justificativa: " ")), HttpStatusCode.BadRequest, "justificativa_obrigatoria");
        await DeveSerProblemaAsync(
            await _analista.PostAsync($"/casos/{casoId}/decisao", Decisao("Talvez")), HttpStatusCode.BadRequest, "decisao_invalida");
        await DeveSerProblemaAsync(
            await _analista.PostAsync($"/casos/{casoId}/atribuicao", new { analista = "" }), HttpStatusCode.BadRequest, "analista_obrigatorio");
    }

    [Fact]
    public async Task CasoJaDecidido_409()
    {
        var (casoId, _) = await _analista.CasoPendenteAsync();
        await _analista.PostAsync($"/casos/{casoId}/atribuicao", new { analista = "Marta" });
        await _analista.PostAsync($"/casos/{casoId}/decisao", Decisao("Aprovar"));

        await DeveSerProblemaAsync(
            await _analista.PostAsync($"/casos/{casoId}/decisao", Decisao("Recusar")), HttpStatusCode.Conflict, "caso_encerrado");
    }

    [Fact]
    public async Task CasoInexistente_404()
    {
        var id = Guid.CreateVersion7();

        await DeveSerProblemaAsync(await _analista.Http.GetAsync($"/casos/{id}", Ct), HttpStatusCode.NotFound, "caso_nao_encontrado");
        await DeveSerProblemaAsync(
            await _analista.PostAsync($"/casos/{id}/atribuicao", new { analista = "Marta" }), HttpStatusCode.NotFound, "caso_nao_encontrado");
    }

    [Fact]
    public async Task FiltroPorStatus_EStatusInvalido()
    {
        var (casoId, _) = await _analista.CasoPendenteAsync();
        await _analista.PostAsync($"/casos/{casoId}/atribuicao", new { analista = "Marta" });
        await _analista.PostAsync($"/casos/{casoId}/decisao", Decisao("Aprovar"));

        var aprovados = await _analista.GetAsync("/casos?status=aprovado&tamanho=200");
        aprovados.GetProperty("itens").EnumerateArray().ShouldContain(caso => caso.GetProperty("id").GetGuid() == casoId);
        var pendentes = await _analista.GetAsync("/casos?tamanho=200");
        pendentes.GetProperty("itens").EnumerateArray().ShouldNotContain(caso => caso.GetProperty("id").GetGuid() == casoId);

        await DeveSerProblemaAsync(await _analista.Http.GetAsync("/casos?status=Talvez", Ct), HttpStatusCode.BadRequest, "filtro_invalido");
    }

    [Fact]
    public async Task SemApiKey_401()
    {
        using var semChave = antifraude.CriarClienteHttp(comApiKey: false);

        (await semChave.GetAsync("/casos", Ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await semChave.GetAsync("/health", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    public void Dispose() => _analista.Dispose();
}
