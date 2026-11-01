using EuroBank.Contas.Domain.ValueObjects;

namespace EuroBank.Contas.Domain.Events;

public sealed record ContaEncerrada(
    ContaId ContaId, int Versao, DateTimeOffset OcorridoEm) : IEventoDeConta;
