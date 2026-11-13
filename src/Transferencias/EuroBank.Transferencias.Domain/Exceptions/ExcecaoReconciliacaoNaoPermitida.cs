using EuroBank.Shared.Exceptions;
using EuroBank.Transferencias.Domain.Enums;

namespace EuroBank.Transferencias.Domain.Exceptions;

public sealed class ExcecaoReconciliacaoNaoPermitida(EstadoTransferencia estado, MotivoTransferencia? motivo)
    : ExcecaoConflito(
        "reconciliacao_nao_permitida",
        $"A transferência está em {estado}{(motivo is null ? string.Empty : $" ({motivo})")}: só é possível reconciliar uma " +
        "transferência externa Divergente porque o banco confirmou a liquidação depois do estorno.");
