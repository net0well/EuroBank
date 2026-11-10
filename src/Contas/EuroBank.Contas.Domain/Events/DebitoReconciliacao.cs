using EuroBank.Contas.Domain.ValueObjects;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Contas.Domain.Events;

/// <summary>Débito de reconciliação (valor e tarifa de uma transferência estornada que o banco liquidou).</summary>
public sealed record DebitoReconciliacao(
    ContaId ContaId,
    Guid TransferenciaId,
    string NomeBeneficiario,
    Dinheiro Valor,
    Saldo SaldoApos,
    int Versao,
    DateTimeOffset OcorridoEm) : IEventoDeConta;
