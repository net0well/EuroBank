using System.Globalization;
using EuroBank.Shared.Exceptions;

namespace EuroBank.Shared.ValueObjects;

/// <summary>
/// Onde o cliente estava ao pedir a operação, como o aplicativo informa: latitude, longitude e o país (ISO 3166, duas
/// letras), sem geocodificação no servidor. No header X-Localizacao: "48.8566,2.3522,FR".
/// </summary>
public sealed record Localizacao
{
    private const double RaioTerraKm = 6_371;

    public Localizacao(double latitude, double longitude, string pais)
    {
        if (double.IsNaN(latitude) || latitude is < -90 or > 90 || double.IsNaN(longitude) || longitude is < -180 or > 180)
        {
            throw new ExcecaoLocalizacaoInvalida("latitude entre -90 e 90 e longitude entre -180 e 180");
        }

        if (pais is not { Length: 2 } || !pais.All(char.IsAsciiLetter))
        {
            throw new ExcecaoLocalizacaoInvalida("país com duas letras (ISO 3166)");
        }

        Latitude = latitude;
        Longitude = longitude;
        Pais = pais.ToUpperInvariant();
    }

    public double Latitude { get; }

    public double Longitude { get; }

    public string Pais { get; }

    /// <summary>Lê o formato do header: "latitude,longitude,país", com ponto decimal.</summary>
    /// <exception cref="ExcecaoLocalizacaoInvalida">Fora do formato ou dos limites.</exception>
    public static Localizacao Ler(string texto)
    {
        var partes = (texto ?? string.Empty).Split(',', StringSplitOptions.TrimEntries);
        if (partes.Length != 3
            || !double.TryParse(partes[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude)
            || !double.TryParse(partes[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude))
        {
            throw new ExcecaoLocalizacaoInvalida("use latitude,longitude,país (ex.: 48.8566,2.3522,FR)");
        }

        return new Localizacao(latitude, longitude, partes[2]);
    }

    /// <summary>Distância em linha reta sobre a superfície da Terra (fórmula de haversine).</summary>
    public double DistanciaKm(Localizacao outra)
    {
        ArgumentNullException.ThrowIfNull(outra);
        var latitude1 = Radianos(Latitude);
        var latitude2 = Radianos(outra.Latitude);
        var deltaLatitude = latitude2 - latitude1;
        var deltaLongitude = Radianos(outra.Longitude - Longitude);
        var a = Math.Pow(Math.Sin(deltaLatitude / 2), 2)
            + (Math.Cos(latitude1) * Math.Cos(latitude2) * Math.Pow(Math.Sin(deltaLongitude / 2), 2));
        return 2 * RaioTerraKm * Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }

    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Latitude},{Longitude},{Pais}");

    private static double Radianos(double graus) => graus * Math.PI / 180;
}
