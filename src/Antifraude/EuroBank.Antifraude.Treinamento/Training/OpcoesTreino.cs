namespace EuroBank.Antifraude.Treinamento.Training;

/// <summary>
/// Como treinar e avaliar. O teste temporal são os dias mais recentes dos dados sintéticos (o treino não os vê). A meta
/// escolhe o limiar recomendado: recall de pelo menos MetaRecall, com no máximo MetaTaxaSinalizada das operações acima
/// do limiar. Abaixo da AUC-PR mínima, a versão não é registrada.
/// </summary>
public sealed record OpcoesTreino(
    int Semente = 42,
    double FracaoTeste = 0.22,
    double MetaRecall = 0.9,
    double MetaTaxaSinalizada = 0.05,
    double AucPrMinima = 0.6,
    int Arvores = 200,
    int Folhas = 31,
    int MinimoPorFolha = 20,
    double TaxaAprendizado = 0.1);
