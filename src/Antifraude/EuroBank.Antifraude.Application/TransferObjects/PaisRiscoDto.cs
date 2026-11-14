namespace EuroBank.Antifraude.Application.TransferObjects;

public sealed record PaisRiscoDto(
    string Codigo,
    DateTimeOffset IncluidoEm);
