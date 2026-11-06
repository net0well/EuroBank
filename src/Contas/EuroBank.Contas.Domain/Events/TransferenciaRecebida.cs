using EuroBank.Contas.Domain.ValueObjects;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Contas.Domain.Events;

public sealed record TransferenciaRecebida(
    ContaId ContaId,
    Guid TransferenciaId,
    Iban IbanOrigem,
    Dinheiro Valor,
    Dinheiro SaldoApos,
    int Versao,
    DateTimeOffset OcorridoEm) : IEventoDeConta;
