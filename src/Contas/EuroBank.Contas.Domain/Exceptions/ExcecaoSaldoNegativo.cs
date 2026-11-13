using EuroBank.Shared.Exceptions;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Contas.Domain.Exceptions;

public sealed class ExcecaoSaldoNegativo(Saldo saldo)
    : ExcecaoRegraNegocio(
        "saldo_negativo",
        $"A conta está com saldo negativo ({saldo} {saldo.Moeda}): só aceita depósitos e créditos até voltar a zero.");
