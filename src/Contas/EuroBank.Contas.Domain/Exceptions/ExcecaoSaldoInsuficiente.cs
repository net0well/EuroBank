using EuroBank.Shared.ValueObjects;
using EuroBank.Shared.Exceptions;

namespace EuroBank.Contas.Domain.Exceptions;

public sealed class ExcecaoSaldoInsuficiente(Dinheiro saldo, Dinheiro valor)
    : ExcecaoRegraNegocio("saldo_insuficiente", $"Saldo insuficiente: saldo {saldo} EUR, valor pedido {valor} EUR.");
