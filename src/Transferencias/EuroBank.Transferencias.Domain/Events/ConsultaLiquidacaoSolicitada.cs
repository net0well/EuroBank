using EuroBank.Transferencias.Domain.ValueObjects;

namespace EuroBank.Transferencias.Domain.Events;

/// <summary>Pergunta ao banco externo se a transferência foi liquidada, antes de decidir pelo estorno.</summary>
public sealed record ConsultaLiquidacaoSolicitada(
    TransferenciaId TransferenciaId,
    int Tentativa,
    DateTimeOffset OcorridoEm) : IEventoDeTransferencia;
