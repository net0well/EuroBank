using System.Net;
using System.Text.Json;
using EuroBank.Extrato.Worker.Tests.Fixtures;

namespace EuroBank.Extrato.Worker.Tests.Endpoints;

/// <summary>Parâmetro que não dá para ler é erro do cliente (400 ProblemDetails), não do servidor.</summary>
public sealed class RequisicaoInvalidaTestes(ExtratoWorkerFixture worker)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task PaginaQueNaoEhNumero_Responde400()
    {
        var resposta = await worker.Http.GetAsync($"/extratos/{Guid.CreateVersion7()}?pagina=primeira", Ct);

        var corpo = await resposta.Content.ReadAsStringAsync(Ct);
        resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest, corpo);
        resposta.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        using var problema = JsonDocument.Parse(corpo);
        problema.RootElement.GetProperty("codigo").GetString().ShouldBe("requisicao_invalida");
    }
}
