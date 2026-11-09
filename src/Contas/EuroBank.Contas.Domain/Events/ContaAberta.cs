using EuroBank.Contas.Domain.ValueObjects;
using EuroBank.Shared.Enums;

namespace EuroBank.Contas.Domain.Events;

public sealed record ContaAberta(
    ContaId ContaId, Iban Iban, Titular Titular, Moeda Moeda, int Versao, DateTimeOffset OcorridoEm) : IEventoDeConta;
