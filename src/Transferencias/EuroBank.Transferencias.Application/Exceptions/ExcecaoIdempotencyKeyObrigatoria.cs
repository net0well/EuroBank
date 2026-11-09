using EuroBank.Shared.Exceptions;

namespace EuroBank.Transferencias.Application.Exceptions;

public sealed class ExcecaoIdempotencyKeyObrigatoria()
    : ExcecaoValidacao(
        "idempotency_key_obrigatoria",
        $"Envie o header Idempotency-Key (até {TamanhoMaximo} caracteres).")
{
    public const int TamanhoMaximo = 100;
}
