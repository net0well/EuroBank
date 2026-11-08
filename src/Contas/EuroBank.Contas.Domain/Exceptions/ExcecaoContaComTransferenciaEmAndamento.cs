using EuroBank.Shared.Exceptions;

namespace EuroBank.Contas.Domain.Exceptions;

public sealed class ExcecaoContaComTransferenciaEmAndamento()
    : ExcecaoRegraNegocio("conta_com_transferencia_em_andamento",
        "A conta tem transferência em andamento e só pode ser encerrada depois que ela terminar.");
