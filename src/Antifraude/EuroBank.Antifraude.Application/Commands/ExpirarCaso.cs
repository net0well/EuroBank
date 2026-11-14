namespace EuroBank.Antifraude.Application.Commands;

/// <summary>O caso passou do prazo sem decisão (a varredura do worker manda um comando por caso vencido).</summary>
public sealed record ExpirarCaso(Guid CasoId);
