using EuroBank.Shared.ValueObjects;

namespace EuroBank.Antifraude.Domain.ValueObjects;

/// <summary>Onde o cliente estava numa operação aprovada da conta, e quando.</summary>
public sealed record LocalizacaoConta(DateTimeOffset Momento, Localizacao Localizacao);
