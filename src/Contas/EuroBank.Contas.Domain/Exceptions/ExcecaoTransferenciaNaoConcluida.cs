using EuroBank.Shared.Exceptions;

namespace EuroBank.Contas.Domain.Exceptions;

public sealed class ExcecaoTransferenciaNaoConcluida(Guid transferenciaId)
    : ExcecaoRegraNegocio(
        "transferencia_nao_concluida",
        $"A transferência {transferenciaId} não foi concluída: não há o que devolver.");
