namespace EuroBank.Antifraude.Painel.TransferObjects;

/// <summary>Um país na lista de países de risco.</summary>
public sealed record PaisRisco(
    string Codigo,
    DateTimeOffset IncluidoEm);
