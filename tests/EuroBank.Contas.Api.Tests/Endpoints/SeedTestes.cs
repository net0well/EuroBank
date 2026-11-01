using System.Text.Json;
using EuroBank.Contas.Api.Tests.Fixtures;

namespace EuroBank.Contas.Api.Tests.Endpoints;

public sealed class SeedTestes(PostgresFixture banco)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task BancoVazioEmDevelopment_AplicaMigrationsECriaTresContasUmaVezSo()
    {
        var connectionString = await banco.CriarBancoVazioAsync();

        await using (var primeiraSubida = new ContasApiFactory(connectionString))
        {
            var contas = await ListarAsync(primeiraSubida);
            contas.ShouldBe(["Ana", "Bruno", "Carla"], ignoreOrder: true);
        }

        await using var segundaSubida = new ContasApiFactory(connectionString);
        (await ListarAsync(segundaSubida)).Count.ShouldBe(3);
    }

    private static async Task<List<string>> ListarAsync(ContasApiFactory api)
    {
        using var cliente = api.CreateClient();
        using var pagina = JsonDocument.Parse(await cliente.GetStringAsync("/contas?tamanho=200", Ct));
        return pagina.RootElement.GetProperty("itens").EnumerateArray()
            .Select(conta => conta.GetProperty("titular").GetString()!)
            .ToList();
    }
}
