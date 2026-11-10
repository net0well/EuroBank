using EuroBank.Contas.Domain.ValueObjects;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Contas.Domain.Events;

public sealed record DepositoRealizado(
    ContaId ContaId, Dinheiro Valor, Saldo SaldoApos, int Versao, DateTimeOffset OcorridoEm) : IEventoDeConta;
