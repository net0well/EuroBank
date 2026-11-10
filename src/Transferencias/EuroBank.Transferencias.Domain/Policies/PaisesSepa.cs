namespace EuroBank.Transferencias.Domain.Policies;

/// <summary>
/// Países da área SEPA aceitos para transferências em euro: os 27 da União Europeia mais Islândia, Liechtenstein,
/// Noruega, Suíça, Reino Unido, Mônaco, San Marino, Andorra e Vaticano (lista do laboratório).
/// </summary>
public static class PaisesSepa
{
    private static readonly HashSet<string> Codigos = new(StringComparer.Ordinal)
    {
        "AT", "BE", "BG", "HR", "CY", "CZ", "DK", "EE", "FI", "FR", "DE", "GR", "HU", "IE", "IT", "LV", "LT", "LU",
        "MT", "NL", "PL", "PT", "RO", "SK", "SI", "ES", "SE",
        "IS", "LI", "NO", "CH", "GB", "MC", "SM", "AD", "VA",
    };

    public static bool Contem(string pais) => Codigos.Contains(pais);
}
