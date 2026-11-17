using EuroBank.Shared.ValueObjects;

namespace EuroBank.Antifraude.Application.TransferObjects;

/// <summary>De onde a conta fez uma operação aprovada: o dispositivo e a localização, quando o cliente os mandou.</summary>
public sealed record UsoConta(Guid ContaId, Guid AnaliseId, DateTimeOffset Momento, string? DispositivoId, Localizacao? Localizacao);
