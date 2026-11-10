using EuroBank.Shared.Exceptions;

namespace EuroBank.Transferencias.Domain.Exceptions;

public sealed class ExcecaoDestinoNaoEuroBank()
    : ExcecaoRegraNegocio(
        "destino_nao_eurobank",
        "O IBAN de destino não é do EuroBank: use a transferência externa (POST /transferencias-externas).");
