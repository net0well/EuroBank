using System.Net;
using System.Net.Http.Headers;
using EuroBank.Contas.Api.Tests.Fixtures;
using static EuroBank.Contas.Api.Tests.Fixtures.ClienteContas;

namespace EuroBank.Contas.Api.Tests.Endpoints;

/// <summary>Corpo que não dá para ler é erro do cliente (400 ProblemDetails), não do servidor.</summary>
public sealed class RequisicaoInvalidaTestes(PostgresFixture banco)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task JsonMalFormado_Responde400()
    {
        using var cliente = banco.Api.CriarClienteAutenticado();

        var resposta = await cliente.PostAsync("/contas", Json("{\"titular\":"), Ct);

        await DeveSerProblemaAsync(resposta, HttpStatusCode.BadRequest, "requisicao_invalida");
    }

    [Fact]
    public async Task CorpoQueNaoEhUtf8_Responde400()
    {
        using var cliente = banco.Api.CriarClienteAutenticado();

        // "José" em Latin-1: o byte 0xE9 sozinho não é UTF-8 válido.
        var resposta = await cliente.PostAsync(
            "/contas", Json([.. "{\"titular\":\"Jos"u8, 0xE9, .. "\"}"u8]), Ct);

        await DeveSerProblemaAsync(resposta, HttpStatusCode.BadRequest, "requisicao_invalida");
    }

    private static ByteArrayContent Json(string corpo) => Json(System.Text.Encoding.UTF8.GetBytes(corpo));

    private static ByteArrayContent Json(byte[] corpo)
    {
        var conteudo = new ByteArrayContent(corpo);
        conteudo.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return conteudo;
    }
}
