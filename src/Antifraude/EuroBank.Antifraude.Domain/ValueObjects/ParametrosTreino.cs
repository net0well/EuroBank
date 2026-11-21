using EuroBank.Antifraude.Domain.Exceptions;

namespace EuroBank.Antifraude.Domain.ValueObjects;

/// <summary>
/// Como treinar: a semente (do FastTree e, com <see cref="GerarDadosNovos"/>, do mundo sintético), a meta que escolhe o
/// limiar recomendado (recall mínimo com no máximo tantas operações sinalizadas), a AUC-PR mínima para registrar a
/// versão, o tamanho do ensemble e a fração dos dias mais recentes que fica para o teste temporal.
/// </summary>
public sealed record ParametrosTreino(
    int Semente = 42,
    double MetaRecall = 0.9,
    double MetaTaxaSinalizada = 0.05,
    double AucPrMinima = 0.6,
    int Arvores = 200,
    int Folhas = 31,
    int MinimoPorFolha = 20,
    double TaxaAprendizado = 0.1,
    double FracaoTeste = 0.22,
    bool GerarDadosNovos = false)
{
    /// <exception cref="ExcecaoParametrosTreinoInvalidos">Algum valor fora da faixa.</exception>
    public ParametrosTreino Validar()
    {
        Exigir(Semente >= 0, nameof(Semente), "a partir de 0");
        Exigir(MetaRecall is > 0 and <= 1, nameof(MetaRecall), "acima de 0 e até 1");
        Exigir(MetaTaxaSinalizada is > 0 and <= 1, nameof(MetaTaxaSinalizada), "acima de 0 e até 1");
        Exigir(AucPrMinima is >= 0 and <= 1, nameof(AucPrMinima), "entre 0 e 1");
        Exigir(Arvores is >= 10 and <= 2000, nameof(Arvores), "entre 10 e 2000");
        Exigir(Folhas is >= 2 and <= 256, nameof(Folhas), "entre 2 e 256");
        Exigir(MinimoPorFolha is >= 1 and <= 1000, nameof(MinimoPorFolha), "entre 1 e 1000");
        Exigir(TaxaAprendizado is > 0 and <= 1, nameof(TaxaAprendizado), "acima de 0 e até 1");
        Exigir(FracaoTeste is >= 0.05 and <= 0.5, nameof(FracaoTeste), "entre 0,05 e 0,5");
        return this;
    }

    private static void Exigir(bool valido, string parametro, string faixa)
    {
        if (!valido)
        {
            throw new ExcecaoParametrosTreinoInvalidos(char.ToLowerInvariant(parametro[0]) + parametro[1..], faixa);
        }
    }
}
