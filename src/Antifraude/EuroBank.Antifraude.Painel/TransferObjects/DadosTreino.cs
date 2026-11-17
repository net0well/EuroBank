namespace EuroBank.Antifraude.Painel.TransferObjects;

/// <summary>De onde veio a versão e a meta do limiar.</summary>
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
