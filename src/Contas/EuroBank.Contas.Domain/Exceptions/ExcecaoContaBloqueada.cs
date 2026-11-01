using EuroBank.Shared.Exceptions;

namespace EuroBank.Contas.Domain.Exceptions;

public sealed class ExcecaoContaBloqueada()
    : ExcecaoRegraNegocio("conta_bloqueada", "A conta está bloqueada e só pode receber créditos.");
