using EuroBank.Antifraude.Features.Services;
using EuroBank.Antifraude.Features.ValueObjects;

namespace EuroBank.Antifraude.Features.Tests.Services;

public sealed class CalculadoraFeaturesTestes
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 15, 14, 30, 0, TimeSpan.Zero);

    private const string Conhecido = "DE89370400440532013000";

    private const string Novo = "FR7630006000011234567890189";

    private static HistoricoConta Historico(
        decimal? saldo = 1_000m,
        DateTimeOffset? abertaEm = null,
        IReadOnlyList<MovimentoHistorico>? saidas = null,
        IReadOnlyList<MovimentoHistorico>? entradas = null) =>
        new(abertaEm ?? Agora.AddDays(-100), saldo, saidas ?? [], entradas ?? [], new HashSet<string> { Conhecido });

    private static VetorFeatures Calcular(HistoricoConta historico, decimal valor = 100m, string? iban = Conhecido, bool paisDeRisco = false) =>
        CalculadoraFeatures.Calcular(historico, new OperacaoAvaliada(Agora, valor, iban, paisDeRisco));

    [Fact]
    public void ValorSobreMedia_UsaSoAsSaidasDos30Dias()
    {
        var historico = Historico(saidas:
        [
            new(Agora.AddDays(-1), 50m, Conhecido),
            new(Agora.AddDays(-10), 150m, Conhecido),
            new(Agora.AddDays(-31), 10_000m, Conhecido),
        ]);

        Calcular(historico, valor: 500m).ValorSobreMedia.ShouldBe(5f);
    }

    [Fact]
    public void SemSaidasNoMes_ValorSobreMediaZero()
    {
        Calcular(Historico(), valor: 500m).ValorSobreMedia.ShouldBe(0f);
    }

    [Fact]
    public void Velocidade_EValoresPequenos_ContamSoAJanelaAntesDoMomento()
    {
        var historico = Historico(saidas:
        [
            new(Agora.AddMinutes(-5), 3m, Novo),
            new(Agora.AddMinutes(-10), 4m, Novo),
            new(Agora.AddMinutes(-20), 5m, Novo),
            new(Agora.AddMinutes(-50), 200m, Conhecido),
            new(Agora.AddMinutes(-70), 200m, Conhecido),
            new(Agora.AddMinutes(1), 1m, Novo),
        ]);

        var features = Calcular(historico);

        features.SaidasUltimaHora.ShouldBe(4f);
        features.PequenasEm15Min.ShouldBe(2f);
    }

    [Theory]
    [InlineData(Conhecido, 0f)]
    [InlineData(Novo, 1f)]
    [InlineData(null, 0f)]
    public void BeneficiarioNovo(string? iban, float esperado)
    {
        Calcular(Historico(), iban: iban).BeneficiarioNovo.ShouldBe(esperado);
    }

    [Theory]
    [InlineData(1_000, 100, 0.1f)]
    [InlineData(100, 5_000, 10f)]
    [InlineData(0, 100, 10f)]
    public void FracaoDoSaldo_ComTeto(int saldo, int valor, float esperado)
    {
        Calcular(Historico(saldo: saldo), valor: valor).FracaoDoSaldo.ShouldBe(esperado, 0.0001f);
    }

    [Fact]
    public void ContaNuncaVista_SemHistoricoESaldoDesconhecido()
    {
        var features = Calcular(HistoricoConta.Vazio, iban: Novo);

        features.SemHistorico.ShouldBe(1f);
        features.IdadeContaDias.ShouldBe(0f);
        features.FracaoDoSaldo.ShouldBe(0f);
        features.BeneficiarioNovo.ShouldBe(1f);
    }

    [Fact]
    public void EntradasSobreValor_SoAsUltimas24h()
    {
        var historico = Historico(entradas:
        [
            new(Agora.AddHours(-2), 3_000m),
            new(Agora.AddHours(-20), 1_000m),
            new(Agora.AddHours(-30), 50_000m),
        ]);

        Calcular(historico, valor: 2_000m).EntradasSobreValor.ShouldBe(2f);
    }

    [Fact]
    public void HoraLogIdadeEPaisDeRisco()
    {
        var features = Calcular(Historico(abertaEm: Agora.AddDays(-12.5)), valor: 999m, paisDeRisco: true);

        features.HoraDoDia.ShouldBe(14f);
        features.LogValor.ShouldBe(3f, 0.0001f);
        features.IdadeContaDias.ShouldBe(12.5f, 0.001f);
        features.PaisDeRisco.ShouldBe(1f);
        features.SemHistorico.ShouldBe(0f);
    }

    [Fact]
    public void Vetor_IdaEVoltaPorArray_ComOsNomesNaMesmaOrdem()
    {
        var features = Calcular(Historico(), valor: 250m, iban: Novo);

        VetorFeatures.DeArray(features.ParaArray()).ShouldBe(features);
        VetorFeatures.Nomes.Count.ShouldBe(features.ParaArray().Length);
    }
}
