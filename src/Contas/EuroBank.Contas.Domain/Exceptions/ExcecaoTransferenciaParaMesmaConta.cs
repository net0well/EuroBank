using EuroBank.Shared.Exceptions;

namespace EuroBank.Contas.Domain.Exceptions;

public sealed class ExcecaoTransferenciaParaMesmaConta()
    : ExcecaoRegraNegocio("transferencia_para_mesma_conta", "A conta de destino deve ser diferente da conta de origem.");
