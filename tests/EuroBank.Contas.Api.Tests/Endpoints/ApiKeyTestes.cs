using System.Net;
using System.Net.Http.Json;
using EuroBank.Contas.Api.Tests.Fixtures;
using static EuroBank.Contas.Api.Tests.Fixtures.ClienteContas;

namespace EuroBank.Contas.Api.Tests.Endpoints;

public sealed class ApiKeyTestes(PostgresFixture banco)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task SemApiKey_EndpointDeNegocioResponde401()
    {
        using var cliente = banco.Api.CreateClient();

        await DeveSerProblemaAsync(await cliente.GetAsync("/contas", Ct), HttpStatusCode.Unauthorized, "api_key_invalida");
        await DeveSerProblemaAsync(
            await cliente.PostAsJsonAsync("/contas", new { titular = "Sem key" }, Ct), HttpStatusCode.Unauthorized, "api_key_invalida");
    }

    [Fact]
    public async Task ComApiKeyErrada_Responde401()
    {
        using var cliente = banco.Api.CreateClient();
        cliente.DefaultRequestHeaders.Add("X-Api-Key", "chave-errada");

        await DeveSerProblemaAsync(await cliente.GetAsync("/contas", Ct), HttpStatusCode.Unauthorized, "api_key_invalida");
    }

    [Fact]
    public async Task HealthESwagger_RespondemSemApiKey_EOSwaggerPublicaOEsquemaDeSeguranca()
    {
        using var cliente = banco.Api.CreateClient();

        (await cliente.GetAsync("/health", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        var swagger = await cliente.GetAsync("/swagger/v1/swagger.json", Ct);
        swagger.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await swagger.Content.ReadAsStringAsync(Ct)).ShouldContain("X-Api-Key");
    }

    [Fact]
    public async Task ComApiKeyCorreta_Atende()
    {
        using var cliente = banco.Api.CriarClienteAutenticado();

        (await cliente.GetAsync("/contas", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
