using EuroBank.Shared.ValueObjects;
using EuroBank.Contas.Domain.ValueObjects;

namespace EuroBank.Contas.Domain.Events;

public sealed record TransferenciaRecebida(
    ContaId ContaId,
    Guid TransferenciaId,
    Iban IbanOrigem,
    Dinheiro Valor,
    Dinheiro SaldoApos,
    int Versao,
    DateTimeOffset OcorridoEm) : IEventoDeConta;
