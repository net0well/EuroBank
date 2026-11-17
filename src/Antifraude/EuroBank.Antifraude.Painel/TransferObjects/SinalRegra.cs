namespace EuroBank.Antifraude.Painel.TransferObjects;

/// <summary>O que uma regra (ou o modelo, com o código ModeloFraude) disse sobre a operação.</summary>
public sealed record SinalRegra(
    string Codigo,
    string Tipo,
    bool Disparou,
    int Pontos,
    bool Sombra,
    string Motivo);
