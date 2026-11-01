using EuroBank.Shared.Exceptions;

namespace EuroBank.Contas.Domain.Exceptions;

public sealed class ExcecaoContaJaBloqueada()
    : ExcecaoRegraNegocio("conta_ja_bloqueada", "A conta já está bloqueada.");
