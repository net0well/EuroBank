namespace EuroBank.Antifraude.Application.TransferObjects;

/// <summary>ContaAberta (eurobank.contas.eventos): a idade da conta conta a partir de OcorridoEm.</summary>
public sealed record EventoContaAberta(
    Guid ContaId,
    string Iban,
    string Titular,
    int Versao,
    DateTimeOffset OcorridoEm,
    string? Moeda = null);
