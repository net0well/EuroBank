using EuroBank.Transferencias.Domain.Enums;

namespace EuroBank.Transferencias.Application.TransferObjects;

public sealed record FiltroTransferencias(Guid? ContaOrigemId, TipoTransferencia? Tipo, EstadoTransferencia? Estado);
