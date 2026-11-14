namespace EuroBank.Antifraude.Application.TransferObjects;

public sealed record SinalRegraDto(
    string Codigo,
    string Tipo,
    bool Disparou,
    int Pontos,
    bool Sombra,
    string Motivo);
