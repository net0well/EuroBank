using EuroBank.Shared.ValueObjects;
using EuroBank.Transferencias.Domain.ValueObjects;

namespace EuroBank.Transferencias.Domain.Events;

/// <summary>Câmbio interno: pede a Contas o crédito do valor convertido na conta de destino.</summary>
public sealed record CreditoSolicitado(
    TransferenciaId TransferenciaId,
    Guid ContaDestinoId,
    Dinheiro Valor,
    string IbanOrigem,
    string NomeOrigem,
    string? Descricao,
    Dinheiro ValorOrigem,
    decimal Taxa,
    int Tentativa,
    DateTimeOffset OcorridoEm) : IEventoDeTransferencia;
