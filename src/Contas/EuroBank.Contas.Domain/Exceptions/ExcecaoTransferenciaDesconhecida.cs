using EuroBank.Shared.Exceptions;

namespace EuroBank.Contas.Domain.Exceptions;

public sealed class ExcecaoTransferenciaDesconhecida(Guid transferenciaId)
    : ExcecaoRegraNegocio("transferencia_desconhecida", $"A conta não tem a transferência {transferenciaId}.");
