using EuroBank.Shared.Enums;
using EuroBank.Shared.ValueObjects;
using EuroBank.Transferencias.Domain.ValueObjects;

namespace EuroBank.Transferencias.Domain.Events;

/// <summary>Pede ao câmbio uma cotação; Valor está na moeda de origem ou na de destino.</summary>
public sealed record CotacaoSolicitada(
    TransferenciaId TransferenciaId,
    Moeda MoedaOrigem,
    Moeda MoedaDestino,
    Dinheiro Valor,
    int Tentativa,
    DateTimeOffset OcorridoEm) : IEventoDeTransferencia;
