using EuroBank.Shared.Exceptions;

namespace EuroBank.Extrato.Domain.Exceptions;

public sealed class ExcecaoExtratoNaoEncontrado(Guid contaId)
    : ExcecaoNaoEncontrado("extrato_nao_encontrado", $"Não existe extrato para a conta {contaId}.");
