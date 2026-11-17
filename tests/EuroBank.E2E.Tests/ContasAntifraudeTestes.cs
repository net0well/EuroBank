using System.Net;
using System.Net.Http.Json;
using EuroBank.E2E.Tests.Fixtures;

namespace EuroBank.E2E.Tests;

/// <summary>
/// Saque e transferência interna em Contas passam pelo antifraude de verdade (gRPC pelo TestServer): o IBAN de destino
/// bloqueado recusa com 422 antifraude_revisao, sem mexer nas contas, e a operação comum segue.
/// </summary>
public sealed class ContasAntifraudeTestes(LaboratorioFixture lab)
{
    private readonly CenarioSaga _cenario = new(lab);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task TransferenciaInternaParaIbanBloqueado_422_SemMexerNasContas_EAComumSegue()
    {
        var origem = await _cenario.AbrirContaEmAsync("EUR", 500m);
        var bloqueado = await _cenario.AbrirContaEmAsync("EUR");
        var livre = await _cenario.AbrirContaEmAsync("EUR");
        (await lab.Antifraude.PostAsJsonAsync("/listas/bloqueio", new { iban = bloqueado.Iban, motivo = "teste", analista = "Marta" }, Ct))
            .EnsureSuccessStatusCode();
        try
        {
            var recusada = await lab.Contas.PostAsJsonAsync($"/contas/{origem.Id}/transferencias", new { ibanDestino = bloqueado.Iban, valor = 100m }, Ct);
            recusada.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
            (await recusada.Content.ReadAsStringAsync(Ct)).ShouldContain("antifraude_revisao");
            (await _cenario.SaldoAsync(origem.Id)).ShouldBe(500m);
            (await _cenario.SaldoAsync(bloqueado.Id)).ShouldBe(0m);

            (await lab.Contas.PostAsJsonAsync($"/contas/{origem.Id}/transferencias", new { ibanDestino = livre.Iban, valor = 100m }, Ct))
                .StatusCode.ShouldBe(HttpStatusCode.OK);
            (await lab.Contas.PostAsJsonAsync($"/contas/{origem.Id}/saques", new { valor = 50m }, Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
            (await _cenario.SaldoAsync(origem.Id)).ShouldBe(350m);
        }
        finally
        {
            await lab.Antifraude.DeleteAsync($"/listas/bloqueio/{bloqueado.Iban}", Ct);
        }
    }
}
