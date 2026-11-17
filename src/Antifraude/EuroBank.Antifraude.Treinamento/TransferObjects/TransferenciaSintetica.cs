using EuroBank.Antifraude.Features.ValueObjects;
using EuroBank.Antifraude.Treinamento.Enums;

namespace EuroBank.Antifraude.Treinamento.TransferObjects;

/// <summary>Uma linha do conjunto de treino: a transferência, as features de antes dela e o rótulo.</summary>
public sealed record TransferenciaSintetica(
    DateTimeOffset Momento,
    Guid ContaId,
    decimal Valor,
    PadraoFraude Padrao,
    VetorFeatures Features)
{
    public bool Fraude => Padrao != PadraoFraude.Nenhum;
}
