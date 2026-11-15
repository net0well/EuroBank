namespace EuroBank.Antifraude.Features.ValueObjects;

/// <summary>
/// As features de uma operação, na ordem que o modelo recebe. Floats, como o ML.NET usa; os nomes são os das colunas
/// do treino e os que aparecem na explicação de cada decisão.
/// </summary>
public sealed record VetorFeatures(
    float ValorSobreMedia,
    float SaidasUltimaHora,
    float PequenasEm15Min,
    float BeneficiarioNovo,
    float IdadeContaDias,
    float HoraDoDia,
    float FracaoDoSaldo,
    float LogValor,
    float PaisDeRisco,
    float SemHistorico,
    float EntradasSobreValor)
{
    public static IReadOnlyList<string> Nomes { get; } =
    [
        nameof(ValorSobreMedia),
        nameof(SaidasUltimaHora),
        nameof(PequenasEm15Min),
        nameof(BeneficiarioNovo),
        nameof(IdadeContaDias),
        nameof(HoraDoDia),
        nameof(FracaoDoSaldo),
        nameof(LogValor),
        nameof(PaisDeRisco),
        nameof(SemHistorico),
        nameof(EntradasSobreValor),
    ];

    public float[] ParaArray() =>
    [
        ValorSobreMedia,
        SaidasUltimaHora,
        PequenasEm15Min,
        BeneficiarioNovo,
        IdadeContaDias,
        HoraDoDia,
        FracaoDoSaldo,
        LogValor,
        PaisDeRisco,
        SemHistorico,
        EntradasSobreValor,
    ];

    public static VetorFeatures DeArray(IReadOnlyList<float> valores)
    {
        ArgumentNullException.ThrowIfNull(valores);
        if (valores.Count != Nomes.Count)
        {
            throw new ArgumentException($"Esperadas {Nomes.Count} features, vieram {valores.Count}.", nameof(valores));
        }

        return new VetorFeatures(
            valores[0], valores[1], valores[2], valores[3], valores[4], valores[5], valores[6], valores[7], valores[8], valores[9], valores[10]);
    }
}
