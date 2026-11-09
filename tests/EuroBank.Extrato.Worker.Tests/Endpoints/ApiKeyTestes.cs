using System.Net;
using EuroBank.Extrato.Worker.Tests.Fixtures;

namespace EuroBank.Extrato.Worker.Tests.Endpoints;

public sealed class ApiKeyTestes(ExtratoWorkerFixture worker)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task SemApiKey_ConsultaDeExtratoResponde401_EHealthResponde200()
    {
        using var cliente = worker.CriarClienteSemChave();

        (await cliente.GetAsync($"/extratos/{Guid.CreateVersion7()}", Ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await cliente.GetAsync("/health", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
