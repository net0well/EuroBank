using EuroBank.Shared.Exceptions;

namespace EuroBank.Transferencias.Domain.Exceptions;

public sealed class ExcecaoCambioNaMesmaMoeda()
    : ExcecaoRegraNegocio(
        "cambio_na_mesma_moeda",
        "As duas contas estão na mesma moeda: use a transferência entre contas (POST /contas/{id}/transferencias).");
