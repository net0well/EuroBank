using EuroBank.Contas.Domain.ValueObjects;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Contas.Domain.Events;

/// <summary>Débito de uma transferência de câmbio para outra conta EuroBank, em outra moeda.</summary>
public sealed record TransferenciaCambioEnviada(
    ContaId ContaId,
    Guid TransferenciaId,
    string IbanDestino,
    string NomeDestino,
    string? Descricao,
    Dinheiro Valor,
    Dinheiro Tarifa,
    Conversao Conversao,
    Saldo SaldoApos,
    int Versao,
    DateTimeOffset OcorridoEm) : IEventoDeConta;
