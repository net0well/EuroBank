using EuroBank.Shared.ValueObjects;
using EuroBank.Transferencias.Domain.ValueObjects;

namespace EuroBank.Transferencias.Domain.Events;

/// <summary>Pede ao banco externo a liquidação do valor (a tarifa fica no EuroBank).</summary>
public sealed record LiquidacaoSolicitada(
    TransferenciaId TransferenciaId,
    string IbanDestino,
    string Bic,
    string NomeBeneficiario,
    Dinheiro Valor,
    int Tentativa,
    DateTimeOffset OcorridoEm) : IEventoDeTransferencia;
