using EuroBank.Shared.Exceptions;

namespace EuroBank.Contas.Domain.Exceptions;

public sealed class ExcecaoTransferenciaJaConcluida(Guid transferenciaId)
    : ExcecaoRegraNegocio("transferencia_ja_concluida", $"A transferência {transferenciaId} já foi concluída e não pode ser estornada.");
