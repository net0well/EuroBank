using System.Net;
using System.Net.Http.Json;
using EuroBank.E2E.Tests.Fixtures;

namespace EuroBank.E2E.Tests;

/// <summary>
/// Antifraude fora do ar para Contas: o saque responde 422 antifraude_indisponivel e o saldo não muda (fail-closed). Roda
/// sozinho: enquanto o antifraude está "fora", nenhum outro saque do laboratório passaria.
/// </summary>
[Collection(nameof(ColecaoSemParalelismo))]
public sealed class ContasAntifraudeForaDoArTestes(LaboratorioFixture lab)
{
    private readonly CenarioSaga _cenario = new(lab);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task AntifraudeForaDoAr_Saque422_SemDebito()
    {
        var conta = await _cenario.AbrirContaAsync(300m);
        lab.AntifraudeForaDoArParaContas = true;
        try
        {
            var resposta = await lab.Contas.PostAsJsonAsync($"/contas/{conta}/saques", new { valor = 100m }, Ct);

            resposta.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
            (await resposta.Content.ReadAsStringAsync(Ct)).ShouldContain("antifraude_indisponivel");
            (await _cenario.SaldoAsync(conta)).ShouldBe(300m);
        }
        finally
        {
            lab.AntifraudeForaDoArParaContas = false;
        }
    }
}
