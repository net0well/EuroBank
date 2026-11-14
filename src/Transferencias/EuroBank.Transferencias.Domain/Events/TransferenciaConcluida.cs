using EuroBank.Shared.ValueObjects;
using EuroBank.Transferencias.Domain.Enums;
using EuroBank.Transferencias.Domain.ValueObjects;

namespace EuroBank.Transferencias.Domain.Events;

/// <summary>Motivo só vem preenchido quando a conclusão saiu do caminho feliz (Reconciliada).</summary>
public sealed record TransferenciaConcluida(
    TransferenciaId TransferenciaId,
    Guid ContaOrigemId,
    Dinheiro Valor,
    Dinheiro Tarifa,
    MotivoTransferencia? Motivo,
    ResumoTransferencia Resumo,
    DateTimeOffset OcorridoEm) : IEventoDeTransferencia;
