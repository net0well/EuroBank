using EuroBank.Shared.Exceptions;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Contas.Domain.Exceptions;

public sealed class ExcecaoSaldoInsuficiente(Dinheiro saldo, Dinheiro valor)
    : ExcecaoRegraNegocio("saldo_insuficiente", $"Saldo insuficiente: saldo {saldo} EUR, valor pedido {valor} EUR.");
