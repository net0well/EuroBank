using EuroBank.E2E.Tests.Fixtures;

namespace EuroBank.E2E.Tests;

/// <summary>
/// Dispositivo e localização no laboratório inteiro: a transferência aprovada ensina a localização ao perfil (o
/// AnaliseDecidida sai pela outbox, passa pelo Debezium e chega ao worker do antifraude), e a seguinte, do outro lado do
/// mundo uma hora depois, vai para a revisão pela viagem impossível.
/// </summary>
public sealed class OrigemAntifraudeTestes(LaboratorioFixture lab)
{
    private const string Paris = "48.8566,2.3522,FR";
    private const string SaoPaulo = "-23.5505,-46.6333,BR";

    private static readonly TimeSpan Limite = TimeSpan.FromSeconds(60);

    private readonly CenarioSaga _cenario = new(lab);

    private readonly CenarioAntifraude _antifraude = new(lab);

    [Fact]
    public async Task AprovadaEmParis_DepoisSaoPaulo_ViagemImpossivel_EmAnaliseManual()
    {
        var conta = await _cenario.AbrirContaAsync(1_000m);
        var primeira = await _cenario.SolicitarDeOrigemComSucessoAsync(conta, "00", 100m, "celular-da-ana", Paris);
        await _cenario.EsperarEstadoAsync(primeira.Id, "Concluida", Limite);
        await Esperar.AteAsync(
            async () => await lab.Redis.GetDatabase().SortedSetLengthAsync($"antifraude:{{{conta}}}:localizacoes") == 1,
            Limite,
            "o worker do antifraude aprender a localização da transferência aprovada");

        var segunda = await _cenario.SolicitarDeOrigemComSucessoAsync(conta, "00", 50m, "celular-da-ana", SaoPaulo);
        var emRevisao = await _cenario.EsperarEstadoAsync(segunda.Id, "EmAnaliseManual", Limite);

        (await _antifraude.RegrasQuePontuaramAsync(emRevisao.AnaliseRiscoId!.Value)).ShouldContain("ViagemImpossivel");
        var caso = await _antifraude.CasoAsync(emRevisao.CasoRevisaoId!.Value);
        caso.GetProperty("analise").GetProperty("localizacao").GetString().ShouldBe(SaoPaulo);
        caso.GetProperty("analise").GetProperty("dispositivoId").GetString().ShouldBe("celular-da-ana");
    }
}
