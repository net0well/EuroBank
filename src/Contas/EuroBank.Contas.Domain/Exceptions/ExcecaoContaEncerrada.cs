using EuroBank.Shared.Exceptions;

namespace EuroBank.Contas.Domain.Exceptions;

public sealed class ExcecaoContaEncerrada()
    : ExcecaoRegraNegocio("conta_encerrada", "A conta está encerrada e não aceita operações.");
