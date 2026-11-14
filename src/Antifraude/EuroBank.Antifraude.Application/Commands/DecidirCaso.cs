namespace EuroBank.Antifraude.Application.Commands;

/// <summary>O analista decide; ao recusar, pode bloquear o IBAN de destino.</summary>
public sealed record DecidirCaso(Guid CasoId, string Analista, bool Aprovar, string Justificativa, bool BloquearIban);
