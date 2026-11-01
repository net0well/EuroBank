using EuroBank.Contas.Domain.ValueObjects;
using EuroBank.Shared.Exceptions;

namespace EuroBank.Contas.Domain.Exceptions;

public sealed class ExcecaoContaNaoEncontrada(ContaId contaId)
    : ExcecaoNaoEncontrado("conta_nao_encontrada", $"A conta {contaId} não existe.");
