using EuroBank.Contas.Domain.ValueObjects;

namespace EuroBank.Contas.Domain.Events;

public sealed record ContaBloqueada(
    ContaId ContaId, int Versao, DateTimeOffset OcorridoEm) : IEventoDeConta;
