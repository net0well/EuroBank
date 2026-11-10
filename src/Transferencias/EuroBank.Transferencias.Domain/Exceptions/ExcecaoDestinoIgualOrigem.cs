using EuroBank.Shared.Exceptions;

namespace EuroBank.Transferencias.Domain.Exceptions;

public sealed class ExcecaoDestinoIgualOrigem()
    : ExcecaoRegraNegocio("destino_igual_origem", "A conta de destino deve ser diferente da conta de origem.");
