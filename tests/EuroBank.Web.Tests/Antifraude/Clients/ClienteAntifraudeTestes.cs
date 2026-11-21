using System.Net;
using System.Text.Json;
using EuroBank.Shared.TransferObjects;
using EuroBank.Web.Antifraude.Clients;
using EuroBank.Web.Antifraude.TransferObjects;
using EuroBank.Web.Tests.Fixtures;

namespace EuroBank.Web.Tests.Antifraude.Clients;

/// <summary>O cliente da REST: as rotas e o JSON que manda, e o ProblemDetails virando uma mensagem para o analista.</summary>
public sealed class ClienteAntifraudeTestes
{
    private readonly ApiFalsa _api = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private ClienteAntifraude Cliente => new(_api.CriarCliente());

    [Fact]
    public async Task ListarCasos_MandaASituacaoEAPaginacao()
    {
        _api.Responder("GET casos", new ResultadoPaginado<CasoResumo>([Exemplos.Resumo()], 1, 50, 1));

        var pagina = await Cliente.ListarCasosAsync("EmAnalise", 1, 50, Ct);

        pagina.Total.ShouldBe(1);
        _api.Pedidos.ShouldHaveSingleItem().Caminho.ShouldBe("casos?status=EmAnalise&pagina=1&tamanho=50");
    }

    [Fact]
    public async Task ListarCasos_SemSituacao_OsPendentes()
    {
        _api.Responder("GET casos", new ResultadoPaginado<CasoResumo>([], 1, 50, 0));

        await Cliente.ListarCasosAsync(string.Empty, 1, 50, Ct);

        _api.Pedidos.ShouldHaveSingleItem().Caminho.ShouldBe("casos?pagina=1&tamanho=50");
    }

    [Fact]
    public async Task Erro_ProblemDetails_ViraExcecaoComOCodigoEAMensagem()
    {
        var caso = Guid.CreateVersion7();
        _api.ResponderProblema($"POST casos/{caso}/atribuicao", HttpStatusCode.Conflict, "caso_de_outro_analista", "O caso está com Bruno.");

        var erro = await Should.ThrowAsync<ExcecaoApiAntifraude>(() => Cliente.AssumirAsync(caso, "Marta", Ct));

        erro.Status.ShouldBe(HttpStatusCode.Conflict);
        erro.Codigo.ShouldBe("caso_de_outro_analista");
        erro.Message.ShouldBe("O caso está com Bruno.");
        JsonDocument.Parse(_api.Pedidos.Single().Corpo!).RootElement.GetProperty("analista").GetString().ShouldBe("Marta");
    }

    [Fact]
    public async Task Erro_SemProblemDetails_MensagemComOStatus()
    {
        var erro = await Should.ThrowAsync<ExcecaoApiAntifraude>(() => Cliente.ObterModeloAsync(99, Ct));

        erro.Status.ShouldBe(HttpStatusCode.NotFound);
        erro.Message.ShouldBe("O antifraude respondeu 404.");
    }

    [Fact]
    public async Task Simular_MandaOJsonDoTranscoding()
    {
        _api.Responder("POST v1/simulacoes", new ResultadoSimulacao("DECISAO_EM_REVISAO", 40, [], 2, Exemplos.Modelo(), null));
        var conta = Guid.CreateVersion7();

        var resultado = await Cliente.SimularAsync(
            new SolicitacaoSimulacao("op-1", "TIPO_OPERACAO_EXTERNA", conta.ToString(), ValorDecimal.De(1_234.56m), "EUR", "DE89370400440532013000"), Ct);

        resultado.Decisao.ShouldBe("DECISAO_EM_REVISAO");
        resultado.Modelo.ShouldNotBeNull().Versao.ShouldBe(3);
        using var corpo = JsonDocument.Parse(_api.Pedidos.Single().Corpo!);
        corpo.RootElement.GetProperty("tipoOperacao").GetString().ShouldBe("TIPO_OPERACAO_EXTERNA");
        corpo.RootElement.GetProperty("contaId").GetString().ShouldBe(conta.ToString());
        corpo.RootElement.GetProperty("valor").GetProperty("units").GetInt64().ShouldBe(1_234);
        corpo.RootElement.GetProperty("valor").GetProperty("nanos").GetInt32().ShouldBe(560_000_000);
    }
}
