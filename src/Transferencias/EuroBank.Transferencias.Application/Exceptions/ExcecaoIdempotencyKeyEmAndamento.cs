using EuroBank.Shared.Exceptions;

namespace EuroBank.Transferencias.Application.Exceptions;

/// <summary>Outro pedido com a mesma Idempotency-Key não terminou a tempo: 409 com Retry-After.</summary>
public sealed class ExcecaoIdempotencyKeyEmAndamento()
    : ExcecaoConflito(
        "idempotency_key_em_andamento",
        "Outro pedido com esta Idempotency-Key ainda está sendo processado; repita em instantes."),
    IExcecaoComNovaTentativa
{
    public TimeSpan TentarDeNovoEm => TimeSpan.FromSeconds(1);
}
