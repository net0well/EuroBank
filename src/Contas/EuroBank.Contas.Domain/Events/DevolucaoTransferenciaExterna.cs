using EuroBank.Contas.Domain.ValueObjects;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Contas.Domain.Events;

/// <summary>Crédito da devolução de uma transferência externa (só o valor; a tarifa não volta).</summary>
public sealed record DevolucaoTransferenciaExterna(
    ContaId ContaId,
    Guid TransferenciaId,
    string NomeBeneficiario,
    Dinheiro Valor,
    Saldo SaldoApos,
    int Versao,
    DateTimeOffset OcorridoEm) : IEventoDeConta;
