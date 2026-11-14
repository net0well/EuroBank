using EuroBank.Shared.Exceptions;

namespace EuroBank.Antifraude.Domain.Exceptions;

/// <summary>O prazo do caso passou, mesmo que a expiração ainda não tenha sido registrada.</summary>
public sealed class ExcecaoCasoExpirado(DateTimeOffset expiraEm)
    : ExcecaoConflito("caso_expirado", $"O caso expirou em {expiraEm:u}.");
