using EuroBank.Shared.Exceptions;
using EuroBank.Transferencias.Domain.Enums;

namespace EuroBank.Transferencias.Domain.Exceptions;

public sealed class ExcecaoCancelamentoNaoPermitido(EstadoTransferencia estado)
    : ExcecaoConflito(
        "cancelamento_nao_permitido",
        $"A transferência está em {estado}: só é possível cancelar antes de a liquidação ser pedida.");
