using EuroBank.Shared.Exceptions;

namespace EuroBank.Transferencias.Domain.Exceptions;

public sealed class ExcecaoTransferenciaNaoEncontrada(Guid id)
    : ExcecaoNaoEncontrado("transferencia_nao_encontrada", $"Transferência {id} não encontrada.");
