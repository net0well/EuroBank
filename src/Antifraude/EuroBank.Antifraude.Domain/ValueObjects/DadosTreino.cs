namespace EuroBank.Antifraude.Domain.ValueObjects;

/// <summary>
/// De onde veio uma versão: a semente dos dados sintéticos, quantas linhas e fraudes, quantos rótulos reais (decisões
/// dos analistas e fraudes confirmadas), o corte temporal entre treino e teste e a meta do limiar recomendado.
/// </summary>
public sealed record DadosTreino(
    int Semente,
    int LinhasSinteticas,
    int FraudesSinteticas,
    int RotulosReais,
    int FraudesReais,
    DateTimeOffset InicioTreino,
    DateTimeOffset CorteTeste,
    DateTimeOffset FimTeste,
    int LinhasTeste,
    double MetaRecall,
    double MetaTaxaSinalizada,
    bool MetaAtingida);
