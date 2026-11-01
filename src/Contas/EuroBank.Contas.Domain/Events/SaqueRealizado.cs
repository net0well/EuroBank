using EuroBank.Shared.ValueObjects;
using EuroBank.Contas.Domain.ValueObjects;

namespace EuroBank.Contas.Domain.Events;

public sealed record SaqueRealizado(
    ContaId ContaId, Dinheiro Valor, Dinheiro SaldoApos, int Versao, DateTimeOffset OcorridoEm) : IEventoDeConta;
