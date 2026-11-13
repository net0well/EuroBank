using EuroBank.Contas.Domain.ValueObjects;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Contas.Domain.Events;

public sealed record TransferenciaEnviada(
    ContaId ContaId,
    Guid TransferenciaId,
    Iban IbanDestino,
    Dinheiro Valor,
    Saldo SaldoApos,
    int Versao,
    DateTimeOffset OcorridoEm) : IEventoDeConta;
