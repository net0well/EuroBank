using EuroBank.Contas.Domain.ValueObjects;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Contas.Domain.Events;

/// <summary>Débito de uma transferência externa; com câmbio, <see cref="Conversao"/> diz o que o destino recebe.</summary>
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
    DateTimeOffset OcorridoEm,
    Conversao? Conversao = null) : IEventoDeConta;
