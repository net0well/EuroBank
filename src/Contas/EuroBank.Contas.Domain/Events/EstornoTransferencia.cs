using EuroBank.Contas.Domain.ValueObjects;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Contas.Domain.Events;

/// <summary>Compensação de uma transferência da saga: <see cref="Valor"/> é o total devolvido (valor + tarifa).</summary>
public sealed record EstornoTransferencia(
    ContaId ContaId,
    Guid TransferenciaId,
    string NomeBeneficiario,
    Dinheiro Valor,
    Saldo SaldoApos,
    int Versao,
    DateTimeOffset OcorridoEm) : IEventoDeConta;
