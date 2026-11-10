using EuroBank.Shared.ValueObjects;
using EuroBank.Transferencias.Domain.Enums;
using EuroBank.Transferencias.Domain.ValueObjects;

namespace EuroBank.Transferencias.Domain.Events;

/// <summary>
/// Pede a Contas o débito do valor e da tarifa, na moeda da conta. Com câmbio, ValorDestino e Taxa dizem o que o
/// destino recebe. Cada reenvio é um evento novo (Tentativa maior).
/// </summary>
public sealed record DebitoSolicitado(
    TransferenciaId TransferenciaId,
    TipoTransferencia Tipo,
    Guid ContaOrigemId,
    Dinheiro Valor,
    Dinheiro Tarifa,
    string IbanDestino,
    string NomeBeneficiario,
    string? Descricao,
    Dinheiro ValorDestino,
    decimal? Taxa,
    int Tentativa,
    DateTimeOffset OcorridoEm) : IEventoDeTransferencia;
