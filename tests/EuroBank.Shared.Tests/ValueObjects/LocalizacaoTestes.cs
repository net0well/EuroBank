using EuroBank.Shared.Exceptions;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Shared.Tests.ValueObjects;

/// <summary>A localização do header X-Localizacao: formato, limites e a distância entre dois pontos.</summary>
public sealed class LocalizacaoTestes
{
    private static readonly Localizacao Paris = new(48.8566, 2.3522, "FR");
    private static readonly Localizacao Londres = new(51.5074, -0.1278, "GB");
    private static readonly Localizacao SaoPaulo = new(-23.5505, -46.6333, "BR");

    [Fact]
    public void Ler_LatitudeLongitudePais_ComPaisEmMaiusculas()
    {
        var localizacao = Localizacao.Ler(" 48.8566 , 2.3522 , fr ");

        localizacao.ShouldBe(Paris);
        localizacao.ToString().ShouldBe("48.8566,2.3522,FR");
        Localizacao.Ler(localizacao.ToString()).ShouldBe(localizacao);
    }

    [Theory]
    [InlineData("48.8,2.3")]
    [InlineData("abc,2.3,FR")]
    [InlineData("48,8,2,FR")]
    [InlineData("91,0,FR")]
    [InlineData("0,181,FR")]
    [InlineData("0,0,F1")]
    [InlineData("0,0,FRA")]
    [InlineData("")]
    public void ForaDoFormatoOuDosLimites_Invalida(string texto)
    {
        Should.Throw<ExcecaoLocalizacaoInvalida>(() => Localizacao.Ler(texto)).Codigo.ShouldBe("localizacao_invalida");
    }

    [Fact]
    public void Distancia_PeloHaversine()
    {
        Paris.DistanciaKm(Paris).ShouldBe(0, tolerance: 1e-9);
        Paris.DistanciaKm(Londres).ShouldBe(343.5, tolerance: 1.5);
        Paris.DistanciaKm(SaoPaulo).ShouldBe(9_403, tolerance: 20);
        SaoPaulo.DistanciaKm(Paris).ShouldBe(Paris.DistanciaKm(SaoPaulo), tolerance: 1e-9);
    }
}
