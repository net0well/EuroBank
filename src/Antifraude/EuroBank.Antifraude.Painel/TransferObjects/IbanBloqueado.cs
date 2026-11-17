namespace EuroBank.Antifraude.Painel.TransferObjects;

/// <summary>Um IBAN na lista de bloqueio.</summary>
public sealed record IbanBloqueado(
    string Iban,
    string? Motivo,
    string BloqueadoPor,
    DateTimeOffset BloqueadoEm);
