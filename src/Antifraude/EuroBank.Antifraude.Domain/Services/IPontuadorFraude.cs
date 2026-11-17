using EuroBank.Antifraude.Domain.ValueObjects;
using EuroBank.Antifraude.Features.ValueObjects;

namespace EuroBank.Antifraude.Domain.Services;

/// <summary>
/// Os modelos carregados (ativo e challenger) pontuando as features de uma operação. Nunca lança: sem modelo, ou com
/// erro na inferência, a pontuação correspondente vem nula e a avaliação segue só com as regras.
/// </summary>
public interface IPontuadorFraude
{
    PontuacoesModelo Pontuar(VetorFeatures features);
}
