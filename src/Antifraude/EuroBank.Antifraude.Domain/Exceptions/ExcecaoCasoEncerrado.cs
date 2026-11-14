using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Shared.Exceptions;

namespace EuroBank.Antifraude.Domain.Exceptions;

/// <summary>O caso já tem decisão, expirou ou foi cancelado.</summary>
public sealed class ExcecaoCasoEncerrado(EstadoCaso estado)
    : ExcecaoConflito("caso_encerrado", $"O caso já foi encerrado ({estado}).");
