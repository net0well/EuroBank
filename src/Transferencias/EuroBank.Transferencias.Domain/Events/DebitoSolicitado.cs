using EuroBank.Shared.ValueObjects;
using EuroBank.Transferencias.Domain.ValueObjects;

namespace EuroBank.Transferencias.Domain.Events;

/// <summary>Pede a Contas o débito do valor e da tarifa. Cada reenvio é um evento novo (Tentativa maior).</summary>
public sealed record DebitoSolicitado(
    TransferenciaId TransferenciaId,
    Guid ContaOrigemId,
    Dinheiro Valor,
    Dinheiro Tarifa,
    string IbanDestino,
    string NomeBeneficiario,
    string? Descricao,
    int Tentativa,
    DateTimeOffset OcorridoEm) : IEventoDeTransferencia;
