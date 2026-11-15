using EuroBank.Antifraude.Domain.Services;
using EuroBank.Antifraude.Domain.ValueObjects;
using EuroBank.Antifraude.Features.ValueObjects;

namespace EuroBank.Antifraude.Domain.Tests.Fakes;

/// <summary>Um pontuador que devolve probabilidades fixas e guarda as features que recebeu.</summary>
internal sealed class PontuadorFixo(float? ativa, float? challenger = null) : IPontuadorFraude
{
    public const int VersaoAtiva = 3;
    public const int VersaoChallenger = 4;

    public VetorFeatures? Recebidas { get; private set; }

    public PontuacoesModelo Pontuar(VetorFeatures features)
    {
        Recebidas = features;
        return new PontuacoesModelo(Pontuacao(VersaoAtiva, ativa), Pontuacao(VersaoChallenger, challenger));
    }

    private static PontuacaoModelo? Pontuacao(int versao, float? probabilidade) =>
        probabilidade is { } valor
            ? new PontuacaoModelo(versao, valor, [new ContribuicaoFeature("BeneficiarioNovo", 1.5f), new ContribuicaoFeature("LogValor", -0.2f)])
            : null;
}
