namespace EuroBank.Antifraude.Application.TransferObjects;

public sealed record RegistroAuditoriaDto(
    int Ordem,
    DateTimeOffset Quando,
    string Quem,
    string Acao,
    string? Detalhe);
