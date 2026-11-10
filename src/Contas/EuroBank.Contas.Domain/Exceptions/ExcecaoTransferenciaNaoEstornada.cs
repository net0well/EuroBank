using EuroBank.Shared.Exceptions;

namespace EuroBank.Contas.Domain.Exceptions;

public sealed class ExcecaoTransferenciaNaoEstornada(Guid transferenciaId)
    : ExcecaoRegraNegocio(
        "transferencia_nao_estornada",
        $"A transferência {transferenciaId} não foi estornada: não há o que reconciliar.");
