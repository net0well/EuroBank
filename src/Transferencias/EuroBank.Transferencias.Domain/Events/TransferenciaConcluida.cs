using EuroBank.Shared.ValueObjects;
using EuroBank.Transferencias.Domain.ValueObjects;

namespace EuroBank.Transferencias.Domain.Events;

public sealed record TransferenciaConcluida(
    TransferenciaId TransferenciaId,
    Guid ContaOrigemId,
    Dinheiro Valor,
    Dinheiro Tarifa,
    DateTimeOffset OcorridoEm) : IEventoDeTransferencia;
