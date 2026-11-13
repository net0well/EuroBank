using EuroBank.Transferencias.Domain.ValueObjects;

namespace EuroBank.Transferencias.Domain.Events;

/// <summary>O banco externo devolveu a transferência: pede a Contas que credite o valor (sem a tarifa) na origem.</summary>
public sealed record CreditoDevolucaoSolicitado(
    TransferenciaId TransferenciaId,
    Guid ContaOrigemId,
    int Tentativa,
    DateTimeOffset OcorridoEm) : IEventoDeTransferencia;
