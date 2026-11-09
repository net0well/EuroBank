using EuroBank.Transferencias.Domain.ValueObjects;

namespace EuroBank.Transferencias.Domain.Events;

/// <summary>Fim feliz: a conta pode voltar a ser encerrada. Contas não responde a este comando.</summary>
public sealed record LiberacaoTravaSolicitada(
    TransferenciaId TransferenciaId,
    Guid ContaOrigemId,
    DateTimeOffset OcorridoEm) : IEventoDeTransferencia;
