using EuroBank.Shared.Exceptions;

namespace EuroBank.Transferencias.Application.Exceptions;

/// <summary>Reconciliação de uma transferência cuja conta de origem já foi encerrada: não há de onde debitar.</summary>
public sealed class ExcecaoContaOrigemEncerrada(Guid contaId)
    : ExcecaoRegraNegocio(
        "conta_origem_encerrada",
        $"A conta {contaId} foi encerrada: a divergência precisa de tratamento manual.");
