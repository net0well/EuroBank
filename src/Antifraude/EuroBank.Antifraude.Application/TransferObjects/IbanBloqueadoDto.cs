namespace EuroBank.Antifraude.Application.TransferObjects;

public sealed record IbanBloqueadoDto(
    string Iban,
    string? Motivo,
    string BloqueadoPor,
    DateTimeOffset BloqueadoEm);
