using EuroBank.Shared.ValueObjects;
using EuroBank.Transferencias.Domain.ValueObjects;

namespace EuroBank.Transferencias.Domain.Events;

/// <summary>O valor devolvido pelo banco externo voltou à conta de origem (ValorDevolvido, na moeda da conta).</summary>
public sealed record TransferenciaDevolvida(
    TransferenciaId TransferenciaId,
    Guid ContaOrigemId,
    Dinheiro ValorDevolvido,
    ResumoTransferencia Resumo,
    DateTimeOffset OcorridoEm) : IEventoDeTransferencia;
