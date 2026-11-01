using EuroBank.Contas.Domain.ValueObjects;

namespace EuroBank.Contas.Domain.Events;

public sealed record ContaAberta(
    ContaId ContaId, Iban Iban, Titular Titular, int Versao, DateTimeOffset OcorridoEm) : IEventoDeConta;
