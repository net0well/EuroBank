using EuroBank.Antifraude.Domain.ValueObjects;
using EuroBank.Antifraude.Features.ValueObjects;

namespace EuroBank.Antifraude.Domain.Services;

/// <summary>Nenhum modelo carregado: só as regras pontuam (o motor sem pontuador, nos testes e no worker).</summary>
public sealed class PontuadorSemModelo : IPontuadorFraude
{
    public static PontuadorSemModelo Instancia { get; } = new();

    public PontuacoesModelo Pontuar(VetorFeatures features) => PontuacoesModelo.Nenhuma;
}
