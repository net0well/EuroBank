using System.Net;
using System.Net.Http.Headers;
using EuroBank.Transferencias.Api.Tests.Fixtures;
using static EuroBank.Transferencias.Api.Tests.Fixtures.ClienteTransferencias;

namespace EuroBank.Transferencias.Api.Tests.Endpoints;

/// <summary>Corpo que não dá para ler é erro do cliente (400 ProblemDetails), não do servidor.</summary>
public sealed class RequisicaoInvalidaTestes(PostgresFixture banco)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task JsonMalFormado_Responde400()
    {
        using var cliente = banco.Api.CriarClienteAutenticado();
        using var requisicao = new HttpRequestMessage(HttpMethod.Post, "/transferencias-externas")
        {
            Content = new StringContent("{\"contaOrigemId\":", new MediaTypeHeaderValue("application/json")),
        };
        requisicao.Headers.Add("Idempotency-Key", Guid.CreateVersion7().ToString());

        var resposta = await cliente.SendAsync(requisicao, Ct);

        await DeveSerProblemaAsync(resposta, HttpStatusCode.BadRequest, "requisicao_invalida");
    }

    [Fact]
    public async Task ParametroDeConsultaInvalido_Responde400()
    {
        using var cliente = banco.Api.CriarClienteAutenticado();

        var resposta = await cliente.GetAsync("/transferencias?pagina=primeira", Ct);

        await DeveSerProblemaAsync(resposta, HttpStatusCode.BadRequest, "requisicao_invalida");
    }
}
