using EuroBank.E2E.Tests.Fixtures;

namespace EuroBank.E2E.Tests;

/// <summary>
/// Fail-closed: sem resposta do antifraude até o prazo da análise (6 s neste laboratório), a saga recusa sem debitar.
/// Roda sozinho, porque derrubar o antifraude afetaria as sagas dos outros testes.
/// </summary>
[Collection(nameof(ColecaoSemParalelismo))]
public sealed class AntifraudeForaDoArTestes(LaboratorioFixture lab)
{
    private static readonly TimeSpan Limite = TimeSpan.FromSeconds(60);

    private readonly CenarioSaga _cenario = new(lab);

    [Fact]
    public async Task AntifraudeForaDoAr_RecusaDepoisDoPrazoDaAnalise()
    {
        var conta = await _cenario.AbrirContaAsync(500m);
        lab.AntifraudeForaDoAr = true;
        try
        {
            var pedido = await _cenario.SolicitarComSucessoAsync(conta, "00", 100m);
            var recusada = await _cenario.EsperarEstadoAsync(pedido.Id, "Recusada", Limite);

            recusada.Motivo.ShouldBe("AntifraudeIndisponivel");
            recusada.PassouPor("Antifraude indisponível (Unavailable)").ShouldBeTrue();
            recusada.AnaliseRiscoId.ShouldBeNull();
        }
        finally
        {
            lab.AntifraudeForaDoAr = false;
        }

        (await _cenario.SaldoAsync(conta)).ShouldBe(500m);
    }
}
