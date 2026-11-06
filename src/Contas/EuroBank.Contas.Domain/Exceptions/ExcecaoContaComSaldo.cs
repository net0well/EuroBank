using EuroBank.Shared.Exceptions;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Contas.Domain.Exceptions;

public sealed class ExcecaoContaComSaldo(Dinheiro saldo)
    : ExcecaoRegraNegocio("conta_com_saldo", $"A conta só pode ser encerrada com saldo zero (saldo atual {saldo} EUR).");
