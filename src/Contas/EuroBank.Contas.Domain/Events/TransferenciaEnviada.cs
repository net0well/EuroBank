using EuroBank.Shared.ValueObjects;
using EuroBank.Contas.Domain.ValueObjects;

namespace EuroBank.Contas.Domain.Events;

public sealed record TransferenciaEnviada(
    ContaId ContaId,
    Guid TransferenciaId,
    Iban IbanDestino,
    Dinheiro Valor,
    Dinheiro SaldoApos,
    int Versao,
    DateTimeOffset OcorridoEm) : IEventoDeConta;
