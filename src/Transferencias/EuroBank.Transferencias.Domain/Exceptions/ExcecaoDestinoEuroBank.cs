using EuroBank.Shared.Exceptions;

namespace EuroBank.Transferencias.Domain.Exceptions;

public sealed class ExcecaoDestinoEuroBank()
    : ExcecaoRegraNegocio(
        "destino_eurobank",
        "O IBAN de destino é do EuroBank: use a transferência entre contas (POST /contas/{id}/transferencias).");
