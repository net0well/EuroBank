using EuroBank.Shared.Exceptions;

namespace EuroBank.Transferencias.Application.Exceptions;

public sealed class ExcecaoIdempotencyKeyReutilizada()
    : ExcecaoRegraNegocio(
        "idempotency_key_reutilizada",
        "Esta Idempotency-Key já foi usada num pedido com outro conteúdo.");
