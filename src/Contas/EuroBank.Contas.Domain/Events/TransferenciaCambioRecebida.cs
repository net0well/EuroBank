using EuroBank.Contas.Domain.ValueObjects;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Contas.Domain.Events;

/// <summary>Crédito de uma transferência de câmbio; <see cref="Conversao"/> diz quanto a origem pagou.</summary>
public sealed record TransferenciaCambioRecebida(
    ContaId ContaId,
    Guid TransferenciaId,
    string IbanOrigem,
    string NomeOrigem,
    string? Descricao,
    Dinheiro Valor,
    Conversao Conversao,
    Saldo SaldoApos,
    int Versao,
    DateTimeOffset OcorridoEm) : IEventoDeConta;
