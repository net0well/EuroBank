using System.Net;
using EuroBank.Transferencias.Api.Tests.Fixtures;
using static EuroBank.Transferencias.Api.Tests.Fixtures.ClienteTransferencias;

namespace EuroBank.Transferencias.Api.Tests.Endpoints;

public sealed class ApiKeyTestes(PostgresFixture banco)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task SemApiKeyOuComKeyErrada_Responde401()
    {
        using var semKey = banco.Api.CreateClient();
        using var keyErrada = banco.Api.CreateClient();
        keyErrada.DefaultRequestHeaders.Add("X-Api-Key", "chave-errada");

        await DeveSerProblemaAsync(await semKey.GetAsync("/transferencias", Ct), HttpStatusCode.Unauthorized, "api_key_invalida");
        await DeveSerProblemaAsync(
            await new ClienteTransferencias(keyErrada).SolicitarAsync(Pedido(), "chave-1"), HttpStatusCode.Unauthorized, "api_key_invalida");
    }

    [Fact]
    public async Task HealthESwagger_RespondemSemApiKey()
    {
        using var cliente = banco.Api.CreateClient();

        (await cliente.GetAsync("/health", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        var swagger = await cliente.GetAsync("/swagger/v1/swagger.json", Ct);
        swagger.StatusCode.ShouldBe(HttpStatusCode.OK);
        var documento = await swagger.Content.ReadAsStringAsync(Ct);
        documento.ShouldContain("X-Api-Key");
        documento.ShouldContain("Idempotency-Key");
    }
}
