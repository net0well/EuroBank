using EuroBank.Antifraude.Domain.Enums;

namespace EuroBank.Antifraude.Domain.ValueObjects;

/// <summary>Uma etapa de uma execução: quando começou, quando terminou (nula enquanto roda) e o que produziu.</summary>
public sealed record EtapaExecutada(EtapaTreino Etapa, DateTimeOffset Inicio, DateTimeOffset? Fim = null, string? Resumo = null);
