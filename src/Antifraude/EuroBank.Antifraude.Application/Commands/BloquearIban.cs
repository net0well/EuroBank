namespace EuroBank.Antifraude.Application.Commands;

public sealed record BloquearIban(string Iban, string? Motivo, string Analista);
