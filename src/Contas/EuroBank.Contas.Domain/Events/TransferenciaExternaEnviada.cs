using EuroBank.Contas.Domain.ValueObjects;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Contas.Domain.Events;

public sealed record TransferenciaExternaEnviada(
    ContaId ContaId,
    Guid TransferenciaId,
    string IbanDestino,
    string NomeBeneficiario,
    string? Descricao,
    Dinheiro Valor,
    Dinheiro Tarifa,
    Dinheiro SaldoApos,
    int Versao,
    DateTimeOffset OcorridoEm) : IEventoDeConta;
